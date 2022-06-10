namespace Chunkyard.Core;

/// <summary>
/// A class which uses a <see cref="IRepository"/> to store snapshots of a set
/// of blobs.
/// </summary>
public class SnapshotStore
{
    public const int LatestSnapshotId = -1;
    public const int SecondLatestSnapshotId = -2;

    private readonly IRepository _repository;
    private readonly FastCdc _fastCdc;
    private readonly IProbe _probe;
    private readonly IClock _clock;
    private readonly Lazy<Crypto> _crypto;
    private readonly ConcurrentDictionary<string, object> _locks;

    private int? _currentSnapshotId;

    public SnapshotStore(
        IRepository repository,
        FastCdc fastCdc,
        IPrompt prompt,
        IProbe probe,
        IClock clock)
    {
        _repository = repository;
        _fastCdc = fastCdc;
        _probe = probe;
        _clock = clock;

        _currentSnapshotId = FetchCurrentSnapshotId();

        _crypto = new Lazy<Crypto>(() =>
        {
            if (_currentSnapshotId == null)
            {
                return new Crypto(
                    prompt.NewPassword(),
                    Crypto.GenerateSalt(),
                    Crypto.DefaultIterations);
            }
            else
            {
                var snapshotReference = GetSnapshotReference(
                    _currentSnapshotId.Value);

                return new Crypto(
                    prompt.ExistingPassword(),
                    snapshotReference.Salt,
                    snapshotReference.Iterations);
            }
        });

        _locks = new ConcurrentDictionary<string, object>();
    }

    public DiffSet StoreSnapshotPreview(IBlobSystem blobSystem)
    {
        ArgumentNullException.ThrowIfNull(blobSystem);

        var blobReferences = _currentSnapshotId == null
            ? Array.Empty<BlobReference>()
            : GetSnapshot(_currentSnapshotId.Value).BlobReferences;

        var blobs = blobSystem.ListBlobs();

        return DiffSet.Create(
            blobReferences.Select(br => br.Blob),
            blobs,
            blob => blob.Name);
    }

    public int StoreSnapshot(IBlobSystem blobSystem)
    {
        ArgumentNullException.ThrowIfNull(blobSystem);

        var newSnapshot = new Snapshot(
            _clock.NowUtc(),
            WriteBlobs(blobSystem));

        var newSnapshotReference = new SnapshotReference(
            _crypto.Value.Salt,
            _crypto.Value.Iterations,
            WriteSnapshot(newSnapshot));

        var newSnapshotId = _currentSnapshotId + 1 ?? 0;

        _repository.Snapshots.StoreValue(
            newSnapshotId,
            Serialize.SnapshotReferenceToBytes(newSnapshotReference));

        _currentSnapshotId = newSnapshotId;

        _probe.StoredSnapshot(newSnapshotId);

        return newSnapshotId;
    }

    public bool CheckSnapshotExists(int snapshotId, Fuzzy includeFuzzy)
    {
        return CheckSnapshot(
            snapshotId,
            includeFuzzy,
            _repository.Chunks.ValueExists);
    }

    public bool CheckSnapshotValid(int snapshotId, Fuzzy includeFuzzy)
    {
        return CheckSnapshot(
            snapshotId,
            includeFuzzy,
            ChunkIdValid);
    }

    public void RestoreSnapshot(
        IBlobSystem blobSystem,
        int snapshotId,
        Fuzzy includeFuzzy)
    {
        _ = FilterSnapshot(snapshotId, includeFuzzy)
            .AsParallel()
            .Select(br => RestoreBlob(blobSystem.NewWrite, br))
            .ToArray();

        _probe.RestoredSnapshot(
            ResolveSnapshotId(snapshotId));
    }

    public DiffSet MirrorSnapshotPreview(
        IBlobSystem blobSystem,
        int snapshotId)
    {
        ArgumentNullException.ThrowIfNull(blobSystem);

        var blobs = blobSystem.ListBlobs();
        var blobReferences = GetSnapshot(snapshotId).BlobReferences;

        return DiffSet.Create(
            blobs,
            blobReferences.Select(br => br.Blob),
            blob => blob.Name);
    }

    public void MirrorSnapshot(
        IBlobSystem blobSystem,
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

        var blobNamesToRemove = blobSystem.ListBlobs()
            .Select(blob => blob.Name)
            .Except(mirroredBlobs.Select(blob => blob.Name));

        foreach (var blobName in blobNamesToRemove)
        {
            blobSystem.RemoveBlob(blobName);
            _probe.RemovedBlob(blobName);
        }
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
        return _repository.Snapshots.ListKeys();
    }

    public IReadOnlyCollection<BlobReference> FilterSnapshot(
        int snapshotId,
        Fuzzy includeFuzzy)
    {
        return GetSnapshot(snapshotId).BlobReferences
            .Where(br => includeFuzzy.IsIncludingMatch(br.Blob.Name))
            .ToArray();
    }

    public void GarbageCollect()
    {
        var usedChunkIds = ListChunkIds(_repository.Snapshots.ListKeys());
        var unusedChunkIds = _repository.Chunks.ListKeys()
            .Except(usedChunkIds);

        foreach (var chunkId in unusedChunkIds)
        {
            _repository.Chunks.RemoveValue(chunkId);
            _probe.RemovedChunk(chunkId);
        }
    }

    public void RemoveSnapshot(int snapshotId)
    {
        var resolvedSnapshotId = ResolveSnapshotId(snapshotId);

        _repository.Snapshots.RemoveValue(resolvedSnapshotId);
        _probe.RemovedSnapshot(resolvedSnapshotId);

        if (_currentSnapshotId == resolvedSnapshotId)
        {
            _currentSnapshotId = FetchCurrentSnapshotId();
        }
    }

    public void KeepSnapshots(int latestCount)
    {
        var snapshotIds = ListSnapshotIds();
        var snapshotIdsToKeep = snapshotIds.TakeLast(latestCount);
        var snapshotIdsToRemove = snapshotIds.Except(snapshotIdsToKeep)
            .ToArray();

        foreach (var snapshotId in snapshotIdsToRemove)
        {
            RemoveSnapshot(snapshotId);
        }
    }

    public void RestoreChunks(
        IEnumerable<string> chunkIds,
        Stream outputStream)
    {
        ArgumentNullException.ThrowIfNull(chunkIds);
        ArgumentNullException.ThrowIfNull(outputStream);

        foreach (var chunkId in chunkIds)
        {
            try
            {
                var decrypted = _crypto.Value.Decrypt(
                    Retrieve(chunkId));

                outputStream.Write(decrypted);
            }
            catch (CryptographicException e)
            {
                throw new ChunkyardException(
                    $"Could not decrypt chunk: {ChunkId.Shorten(chunkId)}",
                    e);
            }
        }
    }

    public void CopyTo(IRepository otherRepository)
    {
        ArgumentNullException.ThrowIfNull(otherRepository);

        var localSnapshotIds = _repository.Snapshots.ListKeys();
        var otherSnapshotIds = otherRepository.Snapshots.ListKeys();

        var otherSnapshotIdMax = otherSnapshotIds.Count == 0
            ? LatestSnapshotId
            : otherSnapshotIds.Max();

        var snapshotIdsToCopy = localSnapshotIds
            .Where(id => id > otherSnapshotIdMax)
            .ToArray();

        var chunkIdsToCopy = ListChunkIds(snapshotIdsToCopy)
            .Except(otherRepository.Chunks.ListKeys())
            .ToArray();

        foreach (var chunkId in chunkIdsToCopy)
        {
            otherRepository.Chunks.StoreValue(
                chunkId,
                RetrieveValidChunk(chunkId));

            _probe.CopiedChunk(chunkId);
        }

        foreach (var snapshotId in snapshotIdsToCopy)
        {
            otherRepository.Snapshots.StoreValue(
                snapshotId,
                Retrieve(snapshotId));

            _probe.CopiedSnapshot(snapshotId);
        }
    }

    private IReadOnlyCollection<string> ListChunkIds(IEnumerable<int> snapshotIds)
    {
        var chunkIds = new HashSet<string>();

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

    private int? FetchCurrentSnapshotId()
    {
        return _repository.Snapshots.ListKeys()
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

        var snapshotIds = _repository.Snapshots.ListKeys()
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
            return Serialize.BytesToSnapshotReference(
                _repository.Snapshots.RetrieveValue(snapshotId));
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
            return Serialize.BytesToSnapshot(
                memoryStream.ToArray());
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not read snapshot: #{snapshotId}",
                e);
        }
    }

    private byte[] Retrieve(string chunkId)
    {
        try
        {
            return _repository.Chunks.RetrieveValue(chunkId);
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not read chunk: {ChunkId.Shorten(chunkId)}",
                e);
        }
    }

    private byte[] Retrieve(int snapshotId)
    {
        try
        {
            return _repository.Snapshots.RetrieveValue(snapshotId);
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not read snapshot reference: #{snapshotId}",
                e);
        }
    }

    private byte[] RetrieveValidChunk(string chunkId)
    {
        var chunk = Retrieve(chunkId);

        if (!ChunkId.Valid(chunkId, chunk))
        {
            throw new ChunkyardException(
                $"Invalid chunk: {ChunkId.Shorten(chunkId)}");
        }

        return chunk;
    }

    private bool ChunkIdValid(string chunkId)
    {
        return _repository.Chunks.ValueExists(chunkId)
            && ChunkId.Valid(
                chunkId,
                _repository.Chunks.RetrieveValue(chunkId));
    }

    private bool CheckSnapshot(
        int snapshotId,
        Fuzzy includeFuzzy,
        Func<string, bool> checkChunkIdFunc)
    {
        var snapshotValid = FilterSnapshot(snapshotId, includeFuzzy)
            .AsParallel()
            .All(br => CheckBlobReference(br, checkChunkIdFunc));

        _probe.SnapshotValid(
            ResolveSnapshotId(snapshotId),
            snapshotValid);

        return snapshotValid;
    }

    private bool CheckBlobReference(
        BlobReference blobReference,
        Func<string, bool> checkChunkIdFunc)
    {
        var blobValid = blobReference.ChunkIds.All(checkChunkIdFunc);

        _probe.BlobValid(blobReference.Blob.Name, blobValid);

        return blobValid;
    }

    private Blob RestoreBlob(
        Func<Blob, Stream> createWriteStream,
        BlobReference blobReference)
    {
        var blob = blobReference.Blob;

        using (var stream = createWriteStream(blob))
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

        return RestoreBlob(blobSystem.OpenWrite, blobReference);
    }

    private BlobReference[] WriteBlobs(IBlobSystem blobSystem)
    {
        _ = _crypto.Value;

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
                ?? Crypto.GenerateNonce();

            using var stream = blobSystem.OpenRead(blob.Name);

            var blobReference = new BlobReference(
                blob,
                nonce,
                WriteChunks(nonce, stream));

            _probe.StoredBlob(blobReference.Blob.Name);

            return blobReference;
        }

        return blobSystem.ListBlobs()
            .AsParallel()
            .AsOrdered()
            .Select(WriteBlob)
            .ToArray();
    }

    private IReadOnlyCollection<string> WriteSnapshot(Snapshot snapshot)
    {
        using var memoryStream = new MemoryStream(
            Serialize.SnapshotToBytes(snapshot));

        return WriteChunks(
            Crypto.GenerateNonce(),
            memoryStream);
    }

    private IReadOnlyCollection<string> WriteChunks(
        byte[] nonce,
        Stream stream)
    {
        string WriteChunk(byte[] chunk)
        {
            var encryptedData = _crypto.Value.Encrypt(nonce, chunk);
            var chunkId = ChunkId.Compute(encryptedData);

            lock (_locks.GetOrAdd(chunkId, _ => new object()))
            {
                if (!_repository.Chunks.ValueExists(chunkId))
                {
                    _repository.Chunks.StoreValue(chunkId, encryptedData);
                }
            }

            return chunkId;
        }

        return _fastCdc.SplitIntoChunks(stream)
            .AsParallel()
            .AsOrdered()
            .Select(WriteChunk)
            .ToArray();
    }
}
