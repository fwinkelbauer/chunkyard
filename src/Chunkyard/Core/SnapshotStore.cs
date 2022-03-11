namespace Chunkyard.Core;

/// <summary>
/// A class which uses <see cref="IRepository{int}"/> and
/// <see cref="IRepository{Uri}"/> to store snapshots of a set of blobs.
/// </summary>
public class SnapshotStore
{
    public const int SchemaVersion = 1;
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

    public DiffSet StoreSnapshotPreview(
        IBlobSystem blobSystem,
        Fuzzy excludeFuzzy)
    {
        ArgumentNullException.ThrowIfNull(blobSystem);

        var blobReferences = _currentSnapshotId == null
            ? Array.Empty<BlobReference>()
            : GetSnapshot(_currentSnapshotId.Value).BlobReferences;

        var blobs = blobSystem.ListBlobs(excludeFuzzy);

        return DiffSet.Create(
            blobReferences.Select(blobReference => blobReference.Blob),
            blobs,
            blob => blob.Name);
    }

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
            CheckChunkIdValid);
    }

    public IReadOnlyCollection<Blob> RestoreSnapshot(
        IBlobSystem blobSystem,
        int snapshotId,
        Fuzzy includeFuzzy)
    {
        var restoredBlob = FilterSnapshot(snapshotId, includeFuzzy)
            .AsParallel()
            .Select(br => RestoreBlob(blobSystem, br))
            .ToArray();

        _probe.RestoredSnapshot(
            ResolveSnapshotId(snapshotId));

        return restoredBlob;
    }

    public DiffSet MirrorSnapshotPreview(
        IBlobSystem blobSystem,
        Fuzzy excludeFuzzy,
        int snapshotId)
    {
        ArgumentNullException.ThrowIfNull(blobSystem);

        var blobs = blobSystem.ListBlobs(excludeFuzzy);
        var blobReferences = GetSnapshot(snapshotId).BlobReferences;

        return DiffSet.Create(
            blobs,
            blobReferences.Select(blobReference => blobReference.Blob),
            blob => blob.Name);
    }

    public IReadOnlyCollection<Blob> MirrorSnapshot(
        IBlobSystem blobSystem,
        Fuzzy excludeFuzzy,
        int snapshotId)
    {
        ArgumentNullException.ThrowIfNull(blobSystem);

        var snapshot = GetSnapshot(snapshotId);

        var mirroredBlobs = snapshot.BlobReferences
            .AsParallel()
            .Select(br => MirrorBlob(blobSystem, br))
            .ToArray();

        _probe.RestoredSnapshot(
            ResolveSnapshotId(snapshotId));

        var blobNamesToRemove = blobSystem.ListBlobs(excludeFuzzy)
            .Select(blob => blob.Name)
            .Except(mirroredBlobs.Select(blob => blob.Name));

        foreach (var blobName in blobNamesToRemove)
        {
            blobSystem.RemoveBlob(blobName);
            _probe.RemovedBlob(blobName);
        }

        return mirroredBlobs;
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

    public IReadOnlyCollection<BlobReference> FilterSnapshot(
        int snapshotId,
        Fuzzy includeFuzzy)
    {
        return GetSnapshot(snapshotId).BlobReferences
            .Where(br => includeFuzzy.IsIncludingMatch(br.Blob.Name))
            .ToArray();
    }

    public IReadOnlyCollection<Uri> GarbageCollect()
    {
        var usedChunkIds = ListChunkIds(_intRepository.ListKeys());
        var unusedChunkIds = _uriRepository.ListKeys()
            .Except(usedChunkIds)
            .ToArray();

        foreach (var chunkId in unusedChunkIds)
        {
            _uriRepository.RemoveValue(chunkId);
            _probe.RemovedChunk(chunkId);
        }

        return unusedChunkIds;
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
            RemoveSnapshot(snapshotId);
        }

        return snapshotIdsToRemove;
    }

    public void RestoreChunks(
        IEnumerable<Uri> chunkIds,
        Stream outputStream)
    {
        ArgumentNullException.ThrowIfNull(chunkIds);
        ArgumentNullException.ThrowIfNull(outputStream);

        foreach (var chunkId in chunkIds)
        {
            try
            {
                var decrypted = _aesGcmCrypto.Value.Decrypt(
                    RetrieveChunk(chunkId));

                outputStream.Write(decrypted);
            }
            catch (CryptographicException e)
            {
                throw new ChunkyardException(
                    $"Could not decrypt chunk: {chunkId}",
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

        var chunkIdsToCopy = ListChunkIds(snapshotIdsToCopy)
            .Except(otherUriRepository.ListKeys())
            .ToArray();

        foreach (var chunkId in chunkIdsToCopy)
        {
            otherUriRepository.StoreValue(
                chunkId,
                RetrieveValidChunk(chunkId));

            _probe.CopiedChunk(chunkId);
        }

        foreach (var snapshotId in snapshotIdsToCopy)
        {
            otherIntRepository.StoreValue(
                snapshotId,
                _intRepository.RetrieveValue(snapshotId));

            _probe.CopiedSnapshot(snapshotId);
        }
    }

    private IEnumerable<Uri> ListChunkIds(IEnumerable<int> snapshotIds)
    {
        var chunkIds = new HashSet<Uri>();

        foreach (var snapshotId in snapshotIds)
        {
            var snapshotReference = GetSnapshotReference(snapshotId);
            var snapshot = GetSnapshot(snapshotReference, snapshotId);

            chunkIds.UnionWith(
                snapshotReference.ChunkIds);

            chunkIds.UnionWith(
                snapshot.BlobReferences.SelectMany(
                    br => br.ChunkIds));
        }

        return chunkIds;
    }

    private bool CheckSnapshot(
        int snapshotId,
        Fuzzy includeFuzzy,
        Func<Uri, bool> checkChunkIdFunc)
    {
        var snapshotValid = FilterSnapshot(snapshotId, includeFuzzy)
            .AsParallel()
            .Select(br => CheckBlobReference(br, checkChunkIdFunc))
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
                $"Could not resolve snapshot reference: #{snapshotId}");
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

        RestoreChunks(
            snapshotReference.ChunkIds,
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

    private byte[] RetrieveChunk(Uri chunkId)
    {
        try
        {
            return _uriRepository.RetrieveValue(chunkId);
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not read chunk: {chunkId}",
                e);
        }
    }

    private byte[] RetrieveValidChunk(Uri chunkId)
    {
        var chunk = RetrieveChunk(chunkId);

        if (!ChunkId.ChunkIdValid(chunkId, chunk))
        {
            throw new ChunkyardException(
                $"Invalid chunk: {chunkId}");
        }

        return chunk;
    }

    private bool CheckChunkIdValid(Uri chunkId)
    {
        try
        {
            return ChunkId.ChunkIdValid(
                chunkId,
                _uriRepository.RetrieveValue(chunkId));
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

        using (var stream = blobSystem.NewWrite(blob))
        {
            RestoreChunks(blobReference.ChunkIds, stream);
        }

        _probe.RestoredBlob(blobReference.Blob.Name);

        return blob;
    }

    private Blob MirrorBlob(
        IBlobSystem blobSystem,
        BlobReference blobReference)
    {
        var blob = blobReference.Blob;

        if (blobSystem.BlobExists(blob.Name)
            && blobSystem.GetBlob(blob.Name).Equals(blob))
        {
            return blob;
        }

        using (var stream = blobSystem.OpenWrite(blob))
        {
            RestoreChunks(blobReference.ChunkIds, stream);
        }

        _probe.RestoredBlob(blobReference.Blob.Name);

        return blob;
    }

    private bool CheckBlobReference(
        BlobReference blobReference,
        Func<Uri, bool> checkChunkIdFunc)
    {
        var blobValid = blobReference.ChunkIds
            .Select(checkChunkIdFunc)
            .Aggregate(true, (total, next) => total && next);

        _probe.BlobValid(blobReference.Blob.Name, blobValid);

        return blobValid;
    }
}
