namespace Chunkyard.Core;

/// <summary>
/// A class which uses <see cref="IRepository{int}"/> and
/// <see cref="IRepository{Uri}"/> to store snapshots of a set of blobs.
/// </summary>
public class SnapshotStore
{
    public const int LatestSnapshotId = -1;
    public const int SecondLatestSnapshotId = -2;

    private const int SchemaVersion = 0;

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

        var snapshotWriter = new SnapshotWriter(
            _uriRepository,
            _fastCdc,
            _probe,
            _aesGcmCrypto.Value);

        var knownBlobReferences = _currentSnapshotId == null
            ? Array.Empty<BlobReference>()
            : GetSnapshot(_currentSnapshotId.Value).BlobReferences;

        var blobReferences = snapshotWriter.WriteBlobs(
            blobSystem,
            excludeFuzzy,
            knownBlobReferences);

        var newSnapshot = new Snapshot(creationTimeUtc, blobReferences);

        var newSnapshotReference = new SnapshotReference(
            SchemaVersion,
            _aesGcmCrypto.Value.Salt,
            _aesGcmCrypto.Value.Iterations,
            snapshotWriter.WriteObject(newSnapshot));

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
        return CheckSnapshot(
            snapshotId,
            includeFuzzy,
            CheckContentUriValid);
    }

    public IReadOnlyCollection<Blob> CleanBlobSystem(
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
            .Where(blob => !blobNamesToKeep.Contains(blob.Name))
            .ToArray();

        foreach (var blob in blobsToRemove)
        {
            blobSystem.RemoveBlob(blob.Name);
            _probe.RemovedBlob(blob);
        }

        return blobsToRemove;
    }

    public IReadOnlyCollection<Blob> RestoreSnapshot(
        IBlobSystem blobSystem,
        int snapshotId,
        Fuzzy includeFuzzy)
    {
        var blobs = ShowSnapshot(snapshotId, includeFuzzy)
            .AsParallel()
            .Select(br => RestoreBlob(blobSystem, br))
            .ToArray();

        _probe.RestoredSnapshot(
            ResolveSnapshotId(snapshotId));

        return blobs;
    }

    public Snapshot GetSnapshot(int snapshotId)
    {
        var resolvedSnapshotId = ResolveSnapshotId(snapshotId);

        return GetSnapshot(
            GetSnapshotReference(resolvedSnapshotId),
            resolvedSnapshotId);
    }

    public IReadOnlyCollection<int> ListSnapshotIds()
    {
        return _intRepository.ListKeys()
            .OrderBy(id => id)
            .ToArray();
    }

    public IReadOnlyCollection<BlobReference> ShowSnapshot(
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
        var snapshotIds = ListSnapshotIds();
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

    public void RestoreContent(
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
                    RetrieveContent(contentUri));

                outputStream.Write(decrypted);
            }
            catch (CryptographicException e)
            {
                throw new ChunkyardException(
                    $"Could not decrypt content: {contentUri}",
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
            .OrderBy(id => id)
            .ToArray();

        var contentUrisToCopy = ListContentUris(snapshotIdsToCopy)
            .Except(otherUriRepository.ListKeys())
            .ToArray();

        foreach (var contentUri in contentUrisToCopy)
        {
            otherUriRepository.StoreValue(
                contentUri,
                RetrieveValidContent(contentUri));

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
            var snapshot = GetSnapshot(snapshotReference, snapshotId);

            contentUris.UnionWith(
                snapshotReference.ContentUris);

            contentUris.UnionWith(
                snapshot.BlobReferences.SelectMany(
                    br => br.ContentUris));
        }

        return contentUris;
    }

    private bool CheckSnapshot(
        int snapshotId,
        Fuzzy includeFuzzy,
        Func<Uri, bool> checkContentUriFunc)
    {
        var snapshotValid = ShowSnapshot(snapshotId, includeFuzzy)
            .AsParallel()
            .Select(br => CheckBlobReference(br, checkContentUriFunc))
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
        else if (snapshotId == LatestSnapshotId
            && _currentSnapshotId != null)
        {
            return _currentSnapshotId.Value;
        }

        var snapshotIds = _intRepository.ListKeys()
            .OrderBy(id => id)
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
            return DataConvert.BytesToVersionedObject<SnapshotReference>(
                _intRepository.RetrieveValue(snapshotId),
                SchemaVersion);
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not read snapshot reference: #{snapshotId}",
                e);
        }
    }

    private Snapshot GetSnapshot(
        SnapshotReference snapshotReference,
        int snapshotId)
    {
        using var memoryStream = new MemoryStream();

        RestoreContent(
            snapshotReference.ContentUris,
            memoryStream);

        try
        {
            return DataConvert.BytesToObject<Snapshot>(
                memoryStream.ToArray());
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not read snapshot: #{snapshotId}",
                e);
        }
    }

    private byte[] RetrieveContent(Uri contentUri)
    {
        try
        {
            return _uriRepository.RetrieveValue(contentUri);
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not read content: {contentUri}",
                e);
        }
    }

    private byte[] RetrieveValidContent(Uri contentUri)
    {
        var content = RetrieveContent(contentUri);

        if (!Id.ContentUriValid(contentUri, content))
        {
            throw new ChunkyardException(
                $"Invalid content: {contentUri}");
        }

        return content;
    }

    private bool CheckContentUriValid(Uri contentUri)
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

    private Blob RestoreBlob(
        IBlobSystem blobSystem,
        BlobReference blobReference)
    {
        var blob = blobReference.Blob;

        if (blobSystem.BlobExists(blob.Name)
            && blobSystem.GetBlob(blob.Name).Equals(blob))
        {
            return blob;
        }

        using var stream = blobSystem.OpenWrite(blob);

        RestoreContent(blobReference.ContentUris, stream);
        _probe.RestoredBlob(blobReference.Blob);

        return blob;
    }

    private bool CheckBlobReference(
        BlobReference blobReference,
        Func<Uri, bool> checkContentUriFunc)
    {
        var blobValid = blobReference.ContentUris
            .Select(checkContentUriFunc)
            .Aggregate(true, (total, next) => total && next);

        _probe.BlobValid(blobReference.Blob, blobValid);

        return blobValid;
    }
}
