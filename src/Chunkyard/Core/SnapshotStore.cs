namespace Chunkyard.Core;

/// <summary>
/// A class which uses an <see cref="IRepository"/> to store snapshots of a set
/// of blobs taken from an <see cref="IBlobSystem"/>.
/// </summary>
public sealed class SnapshotStore
{
    public const int LatestSnapshotId = -1;
    public const int SecondLatestSnapshotId = -2;

    private readonly IRepository _repository;
    private readonly FastCdc _fastCdc;
    private readonly IProbe _probe;
    private readonly IWorld _world;
    private readonly Lazy<Crypto> _crypto;
    private readonly Lazy<uint[]> _gearTable;
    private readonly int _parallelism;

    public SnapshotStore(
        IRepository repository,
        FastCdc fastCdc,
        IProbe probe,
        IWorld world,
        IPrompt prompt,
        int parallelism)
    {
        _repository = repository;
        _fastCdc = fastCdc;
        _probe = probe;
        _world = world;

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
                    _world.GenerateSalt(),
                    Crypto.DefaultIterations);
            }
        });

        _gearTable = new Lazy<uint[]>(
            () => FastCdc.GenerateGearTable(_crypto.Value));

        _parallelism = parallelism;
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
            _world.NowUtc(),
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

    public void EnsureSnapshotExists(int snapshotId, Fuzzy fuzzy)
    {
        var resolvedSnapshotId = ResolveSnapshotId(snapshotId);

        if (!CheckSnapshotExists(resolvedSnapshotId, fuzzy))
        {
            throw new ChunkyardException(
                $"Found missing chunks for snapshot: #{resolvedSnapshotId}");
        }
    }

    public void EnsureSnapshotValid(int snapshotId, Fuzzy fuzzy)
    {
        var resolvedSnapshotId = ResolveSnapshotId(snapshotId);

        if (!CheckSnapshotValid(resolvedSnapshotId, fuzzy))
        {
            throw new ChunkyardException(
                $"Found invalid or missing chunks for snapshot: #{resolvedSnapshotId}");
        }
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
        var diffSet = DiffSet.Create(
            blobSystem.ListBlobs(),
            FilterSnapshot(snapshotId, fuzzy).Select(br => br.Blob),
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
            .WithDegreeOfParallelism(_parallelism)
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
        var unusedChunkIds = _repository.Chunks.List()
            .Except(ListChunkIds(_repository.Snapshots.List()));

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
        var snapshotIdsToRemove = snapshotIds.Take(snapshotIds.Count - latestCount)
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

    public SnapshotReference GetSnapshotReference(int snapshotId)
    {
        var bytes = _repository.Snapshots.Retrieve(
            ResolveSnapshotId(snapshotId));

        try
        {
            return Serialize.BytesToSnapshotReference(bytes);
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not retrieve snapshot: #{snapshotId}",
                e);
        }
    }

    public void CopyTo(IRepository otherRepository, int last = 0)
    {
        var snapshotIds = ListSnapshotIds();
        var otherSnapshotIds = otherRepository.Snapshots.List();

        var sharedSnapshotId = snapshotIds.Intersect(otherSnapshotIds)
            .Max(id => id as int?);

        if (sharedSnapshotId != null)
        {
            var bytes = _repository.Snapshots.Retrieve(sharedSnapshotId.Value);
            var otherBytes = otherRepository.Snapshots.Retrieve(
                sharedSnapshotId.Value);

            if (!bytes.SequenceEqual(otherBytes))
            {
                throw new ChunkyardException(
                    "Copying data between different repositories is not supported");
            }
        }

        var otherSnapshotId = otherSnapshotIds.Max(id => id as int?);

        var snapshotIdsToCopy = otherSnapshotId != null
            ? snapshotIds.Where(id => id > otherSnapshotId)
                .ToArray()
            : snapshotIds;

        if (last > 0)
        {
            snapshotIdsToCopy = snapshotIdsToCopy.TakeLast(last)
                .ToArray();
        }

        var chunkIdsToCopy = ListChunkIds(snapshotIdsToCopy)
            .Except(otherRepository.Chunks.List())
            .ToArray();

        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = _parallelism
        };

        Parallel.ForEach(
            chunkIdsToCopy,
            options,
            chunkId =>
            {
                var bytes = _repository.Chunks.Retrieve(chunkId);

                if (!ChunkId.Valid(chunkId, bytes))
                {
                    throw new ChunkyardException(
                        $"Aborting copy operation. Found invalid chunk: {chunkId}");
                }

                otherRepository.Chunks.Store(chunkId, bytes);
                _probe.CopiedChunk(chunkId);
            });

        Parallel.ForEach(
            snapshotIdsToCopy,
            options,
            snapshotId =>
            {
                var bytes = _repository.Snapshots.Retrieve(snapshotId);

                otherRepository.Snapshots.Store(snapshotId, bytes);
                _probe.CopiedSnapshot(snapshotId);
            });
    }

    private Snapshot GetSnapshot(IReadOnlyCollection<string> chunkIds)
    {
        using var memoryStream = new MemoryStream();

        RestoreChunks(chunkIds, memoryStream);

        try
        {
            return Serialize.BytesToSnapshot(memoryStream.ToArray());
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not read snapshot: {string.Join(", ", chunkIds)}",
                e);
        }
    }

    private BlobReference[] StoreBlobs(IBlobSystem blobSystem)
    {
        var currentBlobReferences = _repository.Snapshots.TryLast(out var snapshotId)
            ? GetSnapshot(snapshotId).BlobReferences
                .ToDictionary(br => br.Blob, br => br)
            : new Dictionary<Blob, BlobReference>();

        BlobReference StoreBlob(Blob blob)
        {
            if (currentBlobReferences.TryGetValue(blob, out var current))
            {
                return current;
            }

            using var stream = blobSystem.OpenRead(blob.Name);

            var blobReference = new BlobReference(
                blob,
                StoreChunks(stream));

            _probe.StoredBlob(blobReference.Blob);

            return blobReference;
        }

        return blobSystem.ListBlobs()
            .AsParallel()
            .AsOrdered()
            .WithDegreeOfParallelism(_parallelism)
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
        return _fastCdc.SplitIntoChunks(stream, _gearTable.Value)
            .AsParallel()
            .AsOrdered()
            .WithDegreeOfParallelism(_parallelism)
            .Select(StoreChunk)
            .ToArray();
    }

    private string StoreChunk(byte[] chunk)
    {
        var encrypted = _crypto.Value.Encrypt(
            _world.GenerateNonce(),
            chunk);

        var chunkId = ChunkId.Compute(encrypted);

        _repository.Chunks.Store(chunkId, encrypted);

        return chunkId;
    }

    private bool CheckSnapshot(
        int snapshotId,
        Fuzzy fuzzy,
        Func<string, bool> checkChunkIdFunc)
    {
        var snapshotValid = FilterSnapshot(snapshotId, fuzzy)
            .AsParallel()
            .WithDegreeOfParallelism(_parallelism)
            .All(br => CheckBlobReference(br, checkChunkIdFunc));

        _probe.SnapshotValid(
            ResolveSnapshotId(snapshotId),
            snapshotValid);

        return snapshotValid;
    }

    private bool ChunkValid(string chunkId)
    {
        return _repository.Chunks.Exists(chunkId)
            && ChunkId.Valid(chunkId, _repository.Chunks.Retrieve(chunkId));
    }

    private bool CheckBlobReference(
        BlobReference blobReference,
        Func<string, bool> checkChunkIdFunc)
    {
        var blobValid = blobReference.ChunkIds.All(checkChunkIdFunc);

        _probe.BlobValid(blobReference.Blob, blobValid);

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

        _probe.RestoredBlob(blobReference.Blob);

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

    private int ResolveSnapshotId(int snapshotId)
    {
        //  0: the first element
        //  1: the second element
        // -2: the second-last element
        // -1: the last element
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
                $"Snapshot does not exist: #{snapshotId}");
        }

        return snapshotIds[position];
    }
}
