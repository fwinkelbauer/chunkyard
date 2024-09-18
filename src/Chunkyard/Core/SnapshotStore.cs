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

    public SnapshotStore(
        IRepository repository,
        FastCdc fastCdc,
        IProbe probe,
        IWorld world,
        IPrompt prompt)
    {
        _repository = repository;
        _fastCdc = fastCdc;
        _probe = probe;
        _world = world;

        _crypto = new(() =>
        {
            if (TryLastSnapshotId(_repository, out var snapshotId))
            {
                var snapshotReference = GetSnapshotReference(snapshotId);

                var promptKey = ToPromptKey(
                    snapshotReference.Salt,
                    snapshotReference.Iterations);

                return new Crypto(
                    prompt.ExistingPassword(promptKey),
                    snapshotReference.Salt,
                    snapshotReference.Iterations);
            }
            else
            {
                var salt = _world.GenerateSalt();
                var iterations = _world.Iterations;
                var promptKey = ToPromptKey(salt, iterations);

                return new Crypto(
                    prompt.NewPassword(promptKey),
                    salt,
                    iterations);
            }
        });
    }

    public DiffSet<Blob> StoreSnapshotPreview(
        IBlobSystem blobSystem,
        Fuzzy? fuzzy = null)
    {
        var blobReferences = TryLastSnapshotId(_repository, out var snapshotId)
            ? GetSnapshot(snapshotId).BlobReferences
            : Array.Empty<BlobReference>();

        fuzzy ??= new();

        var blobs = blobSystem.ListBlobs()
            .Where(b => fuzzy.IsMatch(b.Name))
            .ToArray();

        return DiffSet.Create(
            blobReferences.Select(br => br.Blob),
            blobs,
            blob => blob.Name);
    }

    public int StoreSnapshot(
        IBlobSystem blobSystem,
        Fuzzy? fuzzy = null)
    {
        var snapshot = new Snapshot(
            _world.UtcNow(),
            StoreBlobs(blobSystem, fuzzy));

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

    public bool CheckSnapshotExists(
        int snapshotId,
        Fuzzy? fuzzy = null)
    {
        return CheckSnapshot(
            _repository.Chunks.Exists,
            snapshotId,
            fuzzy);
    }

    public bool CheckSnapshotValid(
        int snapshotId,
        Fuzzy? fuzzy = null)
    {
        return CheckSnapshot(
            ChunkValid,
            snapshotId,
            fuzzy);
    }

    public DiffSet<Blob> RestoreSnapshotPreview(
        IBlobSystem blobSystem,
        int snapshotId,
        Fuzzy? fuzzy = null)
    {
        var diffSet = DiffSet.Create(
            blobSystem.ListBlobs(),
            GetSnapshot(snapshotId).ListBlobs(fuzzy),
            blob => blob.Name);

        return new DiffSet<Blob>(
            diffSet.Added,
            diffSet.Changed,
            Array.Empty<Blob>());
    }

    public void RestoreSnapshot(
        IBlobSystem blobSystem,
        int snapshotId,
        Fuzzy? fuzzy = null)
    {
        var blobReferencesToRestore = GetSnapshot(snapshotId)
            .ListBlobReferences(fuzzy)
            .Where(br => !blobSystem.BlobExists(br.Blob.Name)
                || !blobSystem.GetBlob(br.Blob.Name).Equals(br.Blob))
            .ToArray();

        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = _world.Parallelism
        };

        Parallel.ForEach(
            blobReferencesToRestore,
            options,
            blobReference => RestoreBlob(blobSystem, blobReference));

        _probe.RestoredSnapshot(snapshotId);
    }

    public int[] ListSnapshotIds()
    {
        var snapshotIds = _repository.Snapshots.UnorderedList();

        Array.Sort(snapshotIds);

        return snapshotIds;
    }

    public void GarbageCollect()
    {
        var unusedChunkIds = _repository.Chunks.UnorderedList()
            .Except(ListChunkIds(_repository.Snapshots.UnorderedList()));

        foreach (var chunkId in unusedChunkIds)
        {
            _repository.Chunks.Remove(chunkId);
            _probe.RemovedChunk(chunkId);
        }
    }

    public void RemoveSnapshot(int snapshotId)
    {
        _repository.Snapshots.Remove(ResolveSnapshotId(snapshotId));
        _probe.RemovedSnapshot(snapshotId);
    }

    public void KeepSnapshots(int latestCount)
    {
        var snapshotIds = ListSnapshotIds();

        var snapshotIdsToRemove = snapshotIds
            .Take(snapshotIds.Length - latestCount)
            .ToArray();

        foreach (var snapshotId in snapshotIdsToRemove)
        {
            RemoveSnapshot(snapshotId);
        }
    }

    public void CopyTo(IRepository otherRepository, int last = 0)
    {
        var snapshotIdsToCopy = ListSnapshotIdsToCopy(otherRepository);

        if (last > 0)
        {
            snapshotIdsToCopy = snapshotIdsToCopy
                .TakeLast(last)
                .ToArray();
        }

        var chunkIdsToCopy = ListChunkIds(snapshotIdsToCopy)
            .Except(otherRepository.Chunks.UnorderedList())
            .ToArray();

        CopyChunkIds(otherRepository, chunkIdsToCopy);
        CopySnapshotIds(otherRepository, snapshotIdsToCopy);
    }

    private int[] ListSnapshotIdsToCopy(IRepository otherRepository)
    {
        var snapshotIds = ListSnapshotIds();
        var otherSnapshotIds = otherRepository.Snapshots.UnorderedList();

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
            ? snapshotIds
                .Where(id => id > otherSnapshotId)
                .ToArray()
            : snapshotIds;

        return snapshotIdsToCopy;
    }

    private void CopyChunkIds(
        IRepository otherRepository,
        IEnumerable<string> chunkIdsToCopy)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _world.Parallelism
        };

        Parallel.ForEach(
            chunkIdsToCopy,
            options,
            chunkId =>
            {
                otherRepository.Chunks.Store(
                    chunkId,
                    _repository.Chunks.Retrieve(chunkId));

                _probe.CopiedChunk(chunkId);
            });
    }

    private void CopySnapshotIds(
        IRepository otherRepository,
        IEnumerable<int> snapshotIdsToCopy)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _world.Parallelism
        };

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

    private SnapshotReference GetSnapshotReference(int snapshotId)
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

    private Snapshot GetSnapshot(IEnumerable<string> chunkIds)
    {
        using var memoryStream = new MemoryStream();

        RestoreChunks(chunkIds, memoryStream);

        return Serialize.BytesToSnapshot(memoryStream.ToArray());
    }

    private BlobReference[] StoreBlobs(
        IBlobSystem blobSystem,
        Fuzzy? fuzzy = null)
    {
        fuzzy ??= new();

        var existingBlobReferences = TryLastSnapshotId(_repository, out var snapshotId)
            ? GetSnapshot(snapshotId).BlobReferences
                .ToDictionary(br => br.Blob, br => br)
            : new Dictionary<Blob, BlobReference>();

        var blobs = blobSystem.ListBlobs().Where(b => fuzzy.IsMatch(b.Name));
        var newBlobReferences = new List<BlobReference>();
        var blobsToStore = new List<Blob>();

        foreach (var blob in blobs)
        {
            if (existingBlobReferences.TryGetValue(blob, out var blobReference))
            {
                newBlobReferences.Add(blobReference);
            }
            else
            {
                blobsToStore.Add(blob);
            }
        }

        newBlobReferences.AddRange(blobsToStore
            .AsParallel()
            .WithDegreeOfParallelism(_world.Parallelism)
            .Select(b => StoreBlob(blobSystem, b)));

        return newBlobReferences
            .OrderBy(br => br.Blob.Name)
            .ToArray();
    }

    private string[] StoreSnapshot(Snapshot snapshot)
    {
        using var memoryStream = new MemoryStream(
            Serialize.SnapshotToBytes(snapshot));

        return StoreChunks(memoryStream);
    }

    private int StoreSnapshotReference(SnapshotReference snapshotReference)
    {
        var nextId = TryLastSnapshotId(_repository, out var snapshotId)
            ? snapshotId + 1
            : 0;

        _repository.Snapshots.Store(
            nextId,
            Serialize.SnapshotReferenceToBytes(snapshotReference));

        return nextId;
    }

    private bool CheckSnapshot(
        Func<string, bool> checkChunkIdFunc,
        int snapshotId,
        Fuzzy? fuzzy = null)
    {
        var snapshotValid = GetSnapshot(snapshotId).ListBlobReferences(fuzzy)
            .AsParallel()
            .WithDegreeOfParallelism(_world.Parallelism)
            .All(br => CheckBlobReference(br, checkChunkIdFunc));

        _probe.SnapshotValid(snapshotId, snapshotValid);

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

    private BlobReference StoreBlob(IBlobSystem blobSystem, Blob blob)
    {
        using var stream = blobSystem.OpenRead(blob.Name);

        var blobReference = new BlobReference(
            blob,
            StoreChunks(stream));

        _probe.StoredBlob(blobReference.Blob);

        return blobReference;
    }

    private string[] StoreChunks(Stream stream)
    {
        return _fastCdc.SplitIntoChunks(stream)
            .AsParallel()
            .AsOrdered()
            .WithDegreeOfParallelism(_world.Parallelism)
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

    private void RestoreBlob(
        IBlobSystem blobSystem,
        BlobReference blobReference)
    {
        using (var stream = blobSystem.OpenWrite(blobReference.Blob))
        {
            RestoreChunks(blobReference.ChunkIds, stream);
        }

        _probe.RestoredBlob(blobReference.Blob);
    }

    private void RestoreChunks(
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
            catch (Exception e)
            {
                throw new ChunkyardException(
                    $"Could not restore chunk: {chunkId}",
                    e);
            }
        }
    }

    private HashSet<string> ListChunkIds(IEnumerable<int> snapshotIds)
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

        var snapshotIds = _repository.Snapshots.UnorderedList();
        var position = snapshotIds.Length + snapshotId;

        if (position < 0)
        {
            throw new ChunkyardException(
                $"Snapshot does not exist: #{snapshotId}");
        }

        Array.Sort(snapshotIds);

        return snapshotIds[position];
    }

    private static bool TryLastSnapshotId(IRepository repository, out int key)
    {
        var keys = repository.Snapshots.UnorderedList();
        var any = keys.Length > 0;

        key = any
            ? keys.Max()
            : 0;

        return any;
    }

    private static string ToPromptKey(byte[] salt, int iterations)
    {
        var saltText = Convert.ToHexString(salt)
            .ToLowerInvariant();

        return $"s-{saltText}-i-{iterations}";
    }
}
