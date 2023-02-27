namespace Chunkyard.Core;

/// <summary>
/// A class which uses an <see cref="IRepository"/> to store snapshots of a set
/// of blobs.
/// </summary>
public sealed class SnapshotStore
{
    public const int LatestSnapshotId = -1;
    public const int SecondLatestSnapshotId = -2;

    private readonly IRepository _repository;
    private readonly FastCdc _fastCdc;
    private readonly IProbe _probe;
    private readonly IClock _clock;
    private readonly Lazy<Crypto> _crypto;
    private readonly Lazy<uint[]> _table;

    public SnapshotStore(
        IRepository repository,
        FastCdc fastCdc,
        IProbe probe,
        IClock clock,
        IPrompt prompt)
    {
        _repository = repository;
        _fastCdc = fastCdc;
        _probe = probe;
        _clock = clock;

        _crypto = new Lazy<Crypto>(() =>
        {
            if (_repository.Snapshots.TryLast(out var snapshotId))
            {
                var snapshotReference = GetSnapshotReference(snapshotId);

                return new Crypto(
                    prompt.ExistingPassword(),
                    snapshotReference.Salt,
                    snapshotReference.Iterations);
            }
            else
            {
                return new Crypto(
                    prompt.NewPassword(),
                    Crypto.GenerateSalt(),
                    Crypto.DefaultIterations);
            }
        });

        _table = new Lazy<uint[]>(
            () => FastCdc.GenerateGearTable(_crypto.Value));
    }

    public DiffSet StoreSnapshotPreview(IBlobSystem blobSystem)
    {
        var blobReferences = _repository.Snapshots.TryLast(out var snapshotId)
            ? GetSnapshot(snapshotId).BlobReferences
            : Array.Empty<BlobReference>();

        var blobs = blobSystem.ListBlobs();

        return DiffSet.Create(
            blobReferences.Select(br => br.Blob),
            blobs,
            blob => blob.Name);
    }

    public int StoreSnapshot(IBlobSystem blobSystem)
    {
        var snapshot = new Snapshot(
            _clock.NowUtc(),
            StoreBlobs(blobSystem));

        var snapshotId = StoreSnapshotReference(
            new SnapshotReference(
                _crypto.Value.Salt,
                _crypto.Value.Iterations,
                StoreSnapshot(snapshot)));

        _probe.StoredSnapshot(snapshotId);

        return snapshotId;
    }

    public Snapshot GetSnapshot(int snapshotId)
    {
        return GetSnapshot(
            GetSnapshotReference(snapshotId).ChunkIds);
    }

    public bool CheckSnapshotExists(int snapshotId, Fuzzy fuzzy)
    {
        return CheckSnapshot(
            snapshotId,
            fuzzy,
            _repository.Chunks.Exists);
    }

    public bool CheckSnapshotValid(int snapshotId, Fuzzy fuzzy)
    {
        bool ChunkValid(string chunkId)
        {
            return _repository.Chunks.Exists(chunkId)
                && ChunkId.Valid(
                    chunkId,
                    _repository.Chunks.Retrieve(chunkId));
        }

        return CheckSnapshot(
            snapshotId,
            fuzzy,
            ChunkValid);
    }

    public IReadOnlyCollection<BlobReference> FilterSnapshot(
        int snapshotId,
        Fuzzy fuzzy)
    {
        return GetSnapshot(snapshotId).BlobReferences
            .Where(br => fuzzy.IsMatch(br.Blob.Name))
            .ToArray();
    }

    public DiffSet RestoreSnapshotPreview(
        IBlobSystem blobSystem,
        int snapshotId,
        Fuzzy fuzzy)
    {
        var blobs = blobSystem.ListBlobs();
        var blobReferences = FilterSnapshot(snapshotId, fuzzy);

        var diffSet = DiffSet.Create(
            blobs,
            blobReferences.Select(br => br.Blob),
            blob => blob.Name);

        return new DiffSet(
            diffSet.Added,
            diffSet.Changed,
            Array.Empty<string>());
    }

    public void RestoreSnapshot(
        IBlobSystem blobSystem,
        int snapshotId,
        Fuzzy fuzzy)
    {
        _ = FilterSnapshot(snapshotId, fuzzy)
            .AsParallel()
            .Select(br => RestoreBlob(blobSystem, br))
            .ToArray();

        _probe.RestoredSnapshot(
            ResolveSnapshotId(snapshotId));
    }

    public IReadOnlyCollection<int> ListSnapshotIds()
    {
        var snapshotIds = _repository.Snapshots.List();

        Array.Sort(snapshotIds);

        return snapshotIds;
    }

    public void GarbageCollect()
    {
        var usedChunkIds = ListChunkIds(_repository.Snapshots.List());
        var unusedChunkIds = _repository.Chunks.List()
            .Except(usedChunkIds);

        foreach (var chunkId in unusedChunkIds)
        {
            _repository.Chunks.Remove(chunkId);
            _probe.RemovedChunk(chunkId);
        }
    }

    public void RemoveSnapshot(int snapshotId)
    {
        var resolvedSnapshotId = ResolveSnapshotId(snapshotId);

        _repository.Snapshots.Remove(resolvedSnapshotId);
        _probe.RemovedSnapshot(resolvedSnapshotId);
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
        foreach (var chunkId in chunkIds)
        {
            try
            {
                var decrypted = _crypto.Value.Decrypt(
                    _repository.Chunks.Retrieve(chunkId));

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

    public byte[] RestoreSnapshotReference(int snapshotId)
    {
        return _repository.Snapshots.Retrieve(
            ResolveSnapshotId(snapshotId));
    }

    public void CopyTo(IRepository otherRepository)
    {
        var snapshotIds = ListSnapshotIds();
        var otherSnapshotIds = otherRepository.Snapshots.List();

        var sharedSnapshotId = snapshotIds.Intersect(otherSnapshotIds)
            .Select(id => id as int?)
            .Max();

        if (sharedSnapshotId != null)
        {
            var bytes = _repository.Snapshots.Retrieve(sharedSnapshotId.Value);
            var otherBytes = otherRepository.Snapshots.Retrieve(
                sharedSnapshotId.Value);

            if (!bytes.SequenceEqual(otherBytes))
            {
                throw new ChunkyardException(
                    $"Snapshot reference differs: #{sharedSnapshotId}");
            }
        }

        var otherSnapshotId = otherSnapshotIds
            .Select(id => id as int?)
            .Max();

        var snapshotIdsToCopy = otherSnapshotId != null
            ? snapshotIds.Where(id => id > otherSnapshotId)
                .ToArray()
            : snapshotIds;

        var chunkIdsToCopy = ListChunkIds(snapshotIdsToCopy)
            .Except(otherRepository.Chunks.List())
            .ToArray();

        foreach (var chunkId in chunkIdsToCopy)
        {
            var bytes = _repository.Chunks.Retrieve(chunkId);

            if (!ChunkId.Valid(chunkId, bytes))
            {
                throw new ChunkyardException(
                    $"Invalid chunk: {chunkId}");
            }

            otherRepository.Chunks.Store(chunkId, bytes);
            _probe.CopiedChunk(chunkId);
        }

        foreach (var snapshotId in snapshotIdsToCopy)
        {
            var bytes = _repository.Snapshots.Retrieve(snapshotId);

            otherRepository.Snapshots.Store(snapshotId, bytes);
            _probe.CopiedSnapshot(snapshotId);
        }
    }

    private Snapshot GetSnapshot(IReadOnlyCollection<string> chunkIds)
    {
        using var memoryStream = new MemoryStream();

        RestoreChunks(chunkIds, memoryStream);

        return Serialize.BytesToSnapshot(memoryStream.ToArray());
    }

    private BlobReference[] StoreBlobs(IBlobSystem blobSystem)
    {
        var currentBlobReferences = _repository.Snapshots.TryLast(out var snapshotId)
            ? GetSnapshot(snapshotId).BlobReferences
                .ToDictionary(br => br.Blob.Name, br => br)
            : new Dictionary<string, BlobReference>();

        BlobReference StoreBlob(Blob blob)
        {
            currentBlobReferences.TryGetValue(blob.Name, out var current);

            if (current != null && current.Blob.Equals(blob))
            {
                return current;
            }

            using var stream = blobSystem.OpenRead(blob.Name);

            var blobReference = new BlobReference(
                blob,
                StoreChunks(stream));

            _probe.StoredBlob(blobReference.Blob.Name);

            return blobReference;
        }

        return blobSystem.ListBlobs()
            .AsParallel()
            .AsOrdered()
            .Select(StoreBlob)
            .ToArray();
    }

    private IReadOnlyCollection<string> StoreSnapshot(Snapshot snapshot)
    {
        using var memoryStream = new MemoryStream(
            Serialize.SnapshotToBytes(snapshot));

        return StoreChunks(memoryStream);
    }

    private int StoreSnapshotReference(SnapshotReference snapshotReference)
    {
        var nextId = _repository.Snapshots.TryLast(out var snapshotId)
            ? snapshotId + 1
            : 0;

        _repository.Snapshots.Store(
            nextId,
            Serialize.SnapshotReferenceToBytes(snapshotReference));

        return nextId;
    }

    private IReadOnlyCollection<string> StoreChunks(Stream stream)
    {
        string StoreChunk(byte[] chunk)
        {
            var encrypted = _crypto.Value.Encrypt(
                Crypto.GenerateNonce(),
                chunk);

            var chunkId = ChunkId.Compute(encrypted);

            _repository.Chunks.Store(chunkId, encrypted);

            return chunkId;
        }

        return _fastCdc.SplitIntoChunks(stream, _table.Value)
            .AsParallel()
            .AsOrdered()
            .Select(StoreChunk)
            .ToArray();
    }

    private bool CheckSnapshot(
        int snapshotId,
        Fuzzy fuzzy,
        Func<string, bool> checkChunkIdFunc)
    {
        var snapshotValid = FilterSnapshot(snapshotId, fuzzy)
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

    private IReadOnlyCollection<string> ListChunkIds(
        IEnumerable<int> snapshotIds)
    {
        var chunkIds = new HashSet<string>();

        foreach (var snapshotId in snapshotIds)
        {
            var snapshotChunkIds = GetSnapshotReference(snapshotId).ChunkIds;
            var blobChunkIds = GetSnapshot(snapshotChunkIds).BlobReferences
                .SelectMany(br => br.ChunkIds);

            chunkIds.UnionWith(snapshotChunkIds);
            chunkIds.UnionWith(blobChunkIds);
        }

        return chunkIds;
    }

    private SnapshotReference GetSnapshotReference(int snapshotId)
    {
        var bytes = RestoreSnapshotReference(snapshotId);

        try
        {
            return Serialize.BytesToSnapshotReference(bytes);
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not deserialize snapshot reference: #{snapshotId}",
                e);
        }
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
            && _repository.Snapshots.TryLast(out var lastId))
        {
            return lastId;
        }

        var snapshotIds = _repository.Snapshots.List();

        Array.Sort(snapshotIds);

        var position = snapshotIds.Length + snapshotId;

        if (position < 0)
        {
            throw new ChunkyardException(
                $"Could not resolve snapshot reference: #{snapshotId}");
        }

        return snapshotIds[position];
    }
}
