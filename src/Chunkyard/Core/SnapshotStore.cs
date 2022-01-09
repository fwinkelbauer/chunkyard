namespace Chunkyard.Core;

/// <summary>
/// A class which uses <see cref="IRepository{int}"/> and
/// <see cref="IRepository{Uri}"/> to store snapshots of a set of blobs.
/// </summary>
public class SnapshotStore
{
    public const int LatestSnapshotId = -1;
    public const int SecondLatestSnapshotId = -2;

    private readonly IRepository<Uri> _uriRepository;
    private readonly IRepository<int> _intRepository;
    private readonly FastCdc _fastCdc;
    private readonly IProbe _probe;
    private readonly Lazy<AesGcmCrypto> _aesGcmCrypto;

    private int? _currentSnapshotId;

    public SnapshotStore(
        IRepository<Uri> uriRepository,
        IRepository<int> intRepository,
        FastCdc fastCdc,
        IPrompt prompt,
        IProbe probe)
    {
        _uriRepository = uriRepository;
        _intRepository = intRepository;
        _fastCdc = fastCdc;
        _probe = probe;

        _currentSnapshotId = FetchCurrentSnapshotId();

        _aesGcmCrypto = new Lazy<AesGcmCrypto>(() =>
        {
            if (_currentSnapshotId == null)
            {
                return new AesGcmCrypto(
                    prompt.NewPassword(),
                    AesGcmCrypto.GenerateSalt(),
                    AesGcmCrypto.DefaultIterations);
            }
            else
            {
                var snapshotReference = GetSnapshotReference(
                    _currentSnapshotId.Value);

                return new AesGcmCrypto(
                    prompt.ExistingPassword(),
                    snapshotReference.Salt,
                    snapshotReference.Iterations);
            }
        });
    }

    public bool IsEmpty => _currentSnapshotId == null;

    public int StoreSnapshot(
        IBlobSystem blobSystem,
        Fuzzy excludeFuzzy,
        DateTime creationTimeUtc)
    {
        ArgumentNullException.ThrowIfNull(blobSystem);

        var newSnapshot = new Snapshot(
            creationTimeUtc,
            WriteBlobs(blobSystem, excludeFuzzy));

        using var memoryStream = new MemoryStream(
            DataConvert.ObjectToBytes(newSnapshot));

        var newSnapshotReference = new SnapshotReference(
            _aesGcmCrypto.Value.Salt,
            _aesGcmCrypto.Value.Iterations,
            WriteContent(AesGcmCrypto.GenerateNonce(), memoryStream));

        var newSnapshotId = _currentSnapshotId + 1 ?? 0;

        _intRepository.StoreValue(
            newSnapshotId,
            DataConvert.ObjectToBytes(newSnapshotReference));

        _currentSnapshotId = newSnapshotId;

        _probe.StoredSnapshot(newSnapshotId);

        return newSnapshotId;
    }

    public bool CheckSnapshotExists(int snapshotId, Fuzzy includeFuzzy)
    {
        return CheckSnapshot(
            snapshotId,
            includeFuzzy,
            _uriRepository.ValueExists);
    }

    public bool CheckSnapshotValid(int snapshotId, Fuzzy includeFuzzy)
    {
        bool CheckContentUriValid(Uri contentUri)
        {
            try
            {
                return Id.ContentUriValid(
                    contentUri,
                    _uriRepository.RetrieveValue(contentUri));
            }
            catch (Exception)
            {
                return false;
            }
        }

        return CheckSnapshot(
            snapshotId,
            includeFuzzy,
            CheckContentUriValid);
    }

    public IEnumerable<Blob> CleanBlobSystem(
        IBlobSystem blobSystem,
        Fuzzy excludeFuzzy,
        int snapshotId)
    {
        ArgumentNullException.ThrowIfNull(blobSystem);

        var snapshot = GetSnapshot(snapshotId);
        var blobNamesToKeep = snapshot.BlobReferences
            .Select(br => br.Blob.Name)
            .ToHashSet();

        var blobsToRemove = blobSystem.ListBlobs(excludeFuzzy)
            .Where(blob => !blobNamesToKeep.Contains(blob.Name));

        foreach (var blob in blobsToRemove)
        {
            blobSystem.RemoveBlob(blob.Name);

            _probe.RemovedBlob(blob);
        }

        return blobsToRemove;
    }

    public IEnumerable<Blob> RetrieveSnapshot(
        IBlobSystem blobSystem,
        int snapshotId,
        Fuzzy includeFuzzy)
    {
        Blob RetrieveBlobReference(BlobReference blobReference)
        {
            var blob = blobReference.Blob;

            if (blobSystem.BlobExists(blob.Name)
                && blobSystem.GetBlob(blob.Name).Equals(blob))
            {
                return blob;
            }

            try
            {
                using var stream = blobSystem.OpenWrite(blob);

                RetrieveContent(
                    blobReference.ContentUris,
                    stream);
            }
            catch (ChunkyardException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ChunkyardException(
                    $"Could not restore blob {blobReference.Blob.Name}",
                    e);
            }

            _probe.RetrievedBlob(blobReference.Blob);

            return blob;
        }

        var blobs = ShowSnapshot(snapshotId, includeFuzzy)
            .AsParallel()
            .Select(RetrieveBlobReference)
            .ToArray();

        _probe.RetrievedSnapshot(
            ResolveSnapshotId(snapshotId));

        return blobs;
    }

    public Snapshot GetSnapshot(int snapshotId)
    {
        var resolvedSnapshotId = ResolveSnapshotId(snapshotId);

        return GetSnapshot(
            GetSnapshotReference(resolvedSnapshotId));
    }

    public IEnumerable<int> ListSnapshotIds()
    {
        return _intRepository.ListKeys()
            .OrderBy(i => i);
    }

    public IEnumerable<BlobReference> ShowSnapshot(
        int snapshotId,
        Fuzzy includeFuzzy)
    {
        return GetSnapshot(snapshotId).BlobReferences
            .Where(br => includeFuzzy.IsIncludingMatch(br.Blob.Name))
            .ToArray();
    }

    public IReadOnlyCollection<Uri> GarbageCollect()
    {
        var usedContentUris = ListContentUris(_intRepository.ListKeys());
        var unusedContentUris = _uriRepository.ListKeys()
            .Except(usedContentUris)
            .ToArray();

        foreach (var contentUri in unusedContentUris)
        {
            _uriRepository.RemoveValue(contentUri);

            _probe.RemovedContent(contentUri);
        }

        return unusedContentUris;
    }

    public void RemoveSnapshot(int snapshotId)
    {
        var resolvedSnapshotId = ResolveSnapshotId(snapshotId);

        _intRepository.RemoveValue(resolvedSnapshotId);
        _probe.RemovedSnapshot(resolvedSnapshotId);

        if (_currentSnapshotId == resolvedSnapshotId)
        {
            _currentSnapshotId = FetchCurrentSnapshotId();
        }
    }

    public IReadOnlyCollection<int> KeepSnapshots(int latestCount)
    {
        var snapshotIds = _intRepository.ListKeys()
            .OrderBy(i => i)
            .ToArray();

        var snapshotIdsToKeep = snapshotIds.TakeLast(latestCount);
        var snapshotIdsToRemove = snapshotIds.Except(snapshotIdsToKeep)
            .ToArray();

        foreach (var snapshotId in snapshotIdsToRemove)
        {
            _intRepository.RemoveValue(snapshotId);
            _probe.RemovedSnapshot(snapshotId);
        }

        _currentSnapshotId = FetchCurrentSnapshotId();

        return snapshotIdsToRemove;
    }

    public void RetrieveContent(
        IEnumerable<Uri> contentUris,
        Stream outputStream)
    {
        ArgumentNullException.ThrowIfNull(contentUris);
        ArgumentNullException.ThrowIfNull(outputStream);

        foreach (var contentUri in contentUris)
        {
            try
            {
                var decrypted = _aesGcmCrypto.Value.Decrypt(
                    _uriRepository.RetrieveValue(contentUri));

                outputStream.Write(decrypted);
            }
            catch (CryptographicException e)
            {
                throw new ChunkyardException(
                    $"Could not decrypt content: {contentUri}",
                    e);
            }
            catch (Exception e)
            {
                throw new ChunkyardException(
                    $"Could not read content: {contentUri}",
                    e);
            }
        }
    }

    public void Copy(
        IRepository<Uri> otherUriRepository,
        IRepository<int> otherIntRepository)
    {
        ArgumentNullException.ThrowIfNull(otherUriRepository);
        ArgumentNullException.ThrowIfNull(otherIntRepository);

        var localSnapshotIds = _intRepository.ListKeys();
        var otherSnapshotIds = otherIntRepository.ListKeys();

        var otherSnapshotIdMax = otherSnapshotIds.Count == 0
            ? LatestSnapshotId
            : otherSnapshotIds.Max();

        var snapshotIdsToCopy = localSnapshotIds
            .Where(id => id > otherSnapshotIdMax)
            .ToArray();

        var contentUrisToCopy = ListContentUris(snapshotIdsToCopy)
            .Except(otherUriRepository.ListKeys())
            .ToArray();

        foreach (var contentUri in contentUrisToCopy)
        {
            otherUriRepository.StoreValue(
                contentUri,
                GetValidContent(contentUri));

            _probe.CopiedContent(contentUri);
        }

        foreach (var snapshotId in snapshotIdsToCopy)
        {
            otherIntRepository.StoreValue(
                snapshotId,
                _intRepository.RetrieveValue(snapshotId));

            _probe.CopiedSnapshot(snapshotId);
        }
    }

    private IEnumerable<Uri> ListContentUris(IEnumerable<int> snapshotIds)
    {
        var contentUris = new HashSet<Uri>();

        foreach (var snapshotId in snapshotIds)
        {
            var snapshotReference = GetSnapshotReference(snapshotId);
            var snapshot = GetSnapshot(snapshotReference);

            contentUris.UnionWith(
                snapshotReference.ContentUris);

            contentUris.UnionWith(
                snapshot.BlobReferences.SelectMany(
                    blobReference => blobReference.ContentUris));
        }

        return contentUris;
    }

    private bool CheckSnapshot(
        int snapshotId,
        Fuzzy includeFuzzy,
        Func<Uri, bool> checkContentUriFunc)
    {
        bool CheckBlobReference(BlobReference blobReference)
        {
            var blobValid = blobReference.ContentUris
                .Select(checkContentUriFunc)
                .Aggregate(true, (total, next) => total && next);

            _probe.BlobValid(blobReference.Blob, blobValid);

            return blobValid;
        }

        var snapshotValid = ShowSnapshot(snapshotId, includeFuzzy)
            .AsParallel()
            .Select(CheckBlobReference)
            .Aggregate(true, (total, next) => total && next);

        _probe.SnapshotValid(
            ResolveSnapshotId(snapshotId),
            snapshotValid);

        return snapshotValid;
    }

    private int? FetchCurrentSnapshotId()
    {
        return _intRepository.ListKeys()
            .Select(i => i as int?)
            .Max();
    }

    private IReadOnlyCollection<Uri> WriteContent(
        byte[] nonce,
        Stream stream)
    {
        Uri WriteChunk(byte[] chunk)
        {
            var encryptedData = _aesGcmCrypto.Value.Encrypt(
                nonce,
                chunk);

            var contentUri = Id.ComputeContentUri(encryptedData);

            _uriRepository.StoreValue(contentUri, encryptedData);

            return contentUri;
        }

        return _fastCdc.SplitIntoChunks(stream)
            .AsParallel()
            .AsOrdered()
            .Select(WriteChunk)
            .ToArray();
    }

    private BlobReference[] WriteBlobs(
        IBlobSystem blobSystem,
        Fuzzy excludeFuzzy)
    {
        _ = _aesGcmCrypto.Value;

        var currentBlobReferences = _currentSnapshotId == null
            ? new Dictionary<string, BlobReference>()
            : GetSnapshot(_currentSnapshotId.Value).BlobReferences
                .ToDictionary(br => br.Blob.Name, br => br);

        BlobReference WriteBlob(Blob blob)
        {
            currentBlobReferences.TryGetValue(
                blob.Name,
                out var current);

            if (current != null
                && current.Blob.Equals(blob))
            {
                return current;
            }

            // Known blobs should be encrypted using the same nonce
            var nonce = current?.Nonce
                ?? AesGcmCrypto.GenerateNonce();

            using var stream = blobSystem.OpenRead(blob.Name);

            var blobReference = new BlobReference(
                blob,
                nonce,
                WriteContent(nonce, stream));

            _probe.StoredBlob(blobReference.Blob);

            return blobReference;
        }

        return blobSystem.ListBlobs(excludeFuzzy)
            .AsParallel()
            .Select(WriteBlob)
            .OrderBy(blobReference => blobReference.Blob.Name)
            .ToArray();
    }

    private int ResolveSnapshotId(int snapshotId)
    {
        //  0: the first element
        //  1: the second element
        // -1: the last element
        // -2: the second-last element
        if (snapshotId >= 0)
        {
            return snapshotId;
        }

        var snapshotIds = _intRepository.ListKeys()
            .OrderBy(i => i)
            .ToArray();

        var position = snapshotIds.Length + snapshotId;

        if (position < 0)
        {
            throw new ChunkyardException(
                $"Could not resolve snapshot: #{snapshotId}");
        }

        return snapshotIds[position];
    }

    private SnapshotReference GetSnapshotReference(int snapshotId)
    {
        try
        {
            return DataConvert.BytesToObject<SnapshotReference>(
                _intRepository.RetrieveValue(snapshotId));
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not read snapshot reference: #{snapshotId}",
                e);
        }
    }

    private Snapshot GetSnapshot(SnapshotReference snapshotReference)
    {
        using var memoryStream = new MemoryStream();

        RetrieveContent(
            snapshotReference.ContentUris,
            memoryStream);

        return DataConvert.BytesToObject<Snapshot>(
            memoryStream.ToArray());
    }

    private byte[] GetValidContent(Uri contentUri)
    {
        try
        {
            var content = _uriRepository.RetrieveValue(contentUri);

            if (!Id.ContentUriValid(contentUri, content))
            {
                throw new ChunkyardException(
                    $"Invalid content: {contentUri}");
            }

            return content;
        }
        catch (ChunkyardException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not read content: {contentUri}",
                e);
        }
    }
}
