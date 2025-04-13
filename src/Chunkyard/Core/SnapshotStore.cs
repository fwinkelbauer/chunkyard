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
    private readonly IChunker _chunker;
    private readonly IProbe _probe;
    private readonly Lazy<Crypto> _crypto;

    public SnapshotStore(
        IRepository repository,
        IChunker chunker,
        IProbe probe,
        ICryptoFactory cryptoFactory)
    {
        _repository = repository;
        _chunker = chunker;
        _probe = probe;

        _crypto = new(() =>
        {
            var snapshotReference = TryLastSnapshotId(_repository, out var snapshotId)
                ? GetSnapshotReference(snapshotId)
                : null;

            return cryptoFactory.Create(snapshotReference);
        });
    }

    public int StoreSnapshot(IBlobSystem blobSystem, DateTime utcNow, Fuzzy fuzzy)
    {
        var snapshot = new Snapshot(
            utcNow,
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
            GetSnapshotReference(snapshotId));
    }

    public bool CheckSnapshot(int snapshotId, Fuzzy fuzzy)
    {
        var snapshotValid = GetSnapshot(snapshotId).ListBlobReferences(fuzzy)
            .All(br => CheckBlobReference(br));

        _probe.SnapshotValid(snapshotId, snapshotValid);

        return snapshotValid;
    }

    public void RestoreSnapshot(
        IBlobSystem blobSystem,
        int snapshotId,
        Fuzzy fuzzy)
    {
        var blobReferencesToRestore = GetSnapshot(snapshotId)
            .ListBlobReferences(fuzzy)
            .Where(br => !br.Blob.Equals(blobSystem.GetBlob(br.Blob.Name)));

        foreach (var blobReference in blobReferencesToRestore)
        {
            RestoreBlob(blobSystem, blobReference);
        }

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
        var resolved = ResolveSnapshotId(snapshotId);

        _repository.Snapshots.Remove(resolved);
        _probe.RemovedSnapshot(resolved);
    }

    public void KeepSnapshots(int latestCount)
    {
        var snapshotIds = ListSnapshotIds();

        var snapshotIdsToRemove = snapshotIds
            .Take(snapshotIds.Length - latestCount);

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
            .Except(otherRepository.Chunks.UnorderedList());

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
        foreach (var chunkId in chunkIdsToCopy)
        {
            otherRepository.Chunks.Store(
                chunkId,
                _repository.Chunks.Retrieve(chunkId));

            _probe.CopiedChunk(chunkId);
        }
    }

    private void CopySnapshotIds(
        IRepository otherRepository,
        IEnumerable<int> snapshotIdsToCopy)
    {
        foreach (var snapshotId in snapshotIdsToCopy)
        {
            otherRepository.Snapshots.Store(
                snapshotId,
                _repository.Snapshots.Retrieve(snapshotId));

            _probe.CopiedSnapshot(snapshotId);
        }
    }

    private SnapshotReference GetSnapshotReference(int snapshotId)
    {
        var resolved = ResolveSnapshotId(snapshotId);
        var bytes = _repository.Snapshots.Retrieve(resolved);

        try
        {
            return Serialize.BytesToSnapshotReference(bytes);
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not retrieve snapshot: #{resolved}",
                e);
        }
    }

    private Snapshot GetSnapshot(SnapshotReference snapshotReference)
    {
        using var memoryStream = new MemoryStream();

        RestoreChunks(snapshotReference.ChunkIds, memoryStream);

        return Serialize.BytesToSnapshot(memoryStream.ToArray());
    }

    private BlobReference[] StoreBlobs(
        IBlobSystem blobSystem,
        Fuzzy fuzzy)
    {
        var existingBlobReferences = TryLastSnapshotId(_repository, out var snapshotId)
            ? GetSnapshot(snapshotId).BlobReferences.ToDictionary(br => br.Blob, br => br)
            : new Dictionary<Blob, BlobReference>();

        var blobs = blobSystem.ListBlobs(fuzzy);
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

        newBlobReferences.AddRange(blobsToStore.Select(
            b => StoreBlob(blobSystem, b)));

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

    private bool CheckBlobReference(BlobReference blobReference)
    {
        var blobValid = blobReference.ChunkIds.All(
            chunkId => _repository.Chunks.Exists(chunkId)
                && chunkId.Equals(ToChunkId(_repository.Chunks.Retrieve(chunkId))));

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
        return _chunker.Chunkify(stream)
            .Select(chunk =>
            {
                var encrypted = _crypto.Value.Encrypt(chunk);
                var chunkId = ToChunkId(encrypted);

                _repository.Chunks.Store(chunkId, encrypted);

                return chunkId;
            })
            .ToArray();
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
            var snapshotReference = GetSnapshotReference(snapshotId);

            var blobChunkIds = GetSnapshot(snapshotReference).BlobReferences
                .SelectMany(br => br.ChunkIds);

            chunkIds.UnionWith(snapshotReference.ChunkIds);
            chunkIds.UnionWith(blobChunkIds);
        }

        return chunkIds;
    }

    private int ResolveSnapshotId(int snapshotId)
    {
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

    private static string ToChunkId(ReadOnlySpan<byte> chunk)
    {
        return Convert.ToHexString(SHA256.HashData(chunk))
            .ToLowerInvariant();
    }
}
