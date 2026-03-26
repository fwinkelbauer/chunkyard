namespace Chunkyard.Core;

/// <summary>
/// A class which uses an <see cref="IRepository"/> to store snapshots of a set
/// of blobs taken from an <see cref="IBlobSystem"/>.
/// </summary>
public sealed class SnapshotStore
{
    private readonly IRepository _repository;
    private readonly IProbe _probe;
    private readonly Lazy<Chunker> _chunker;

    public SnapshotStore(
        IRepository repository,
        IProbe probe,
        ICryptoFactory cryptoFactory)
    {
        _repository = repository;
        _probe = probe;

        _chunker = new Lazy<Chunker>(() =>
        {
            var snapshotReference = TryLastSnapshotId(out var snapshotId)
                ? GetSnapshotReference(snapshotId)
                : null;

            var crypto = cryptoFactory.Create(snapshotReference);

            return new Chunker(_repository.Chunks, crypto);
        });
    }

    public int StoreSnapshot(
        IBlobSystem blobSystem,
        Regex? regex = null)
    {
        var snapshotId = 0;
        var previousBlobReferences = new Dictionary<Blob, BlobReference>();

        if (TryLastSnapshotId(out var previousSnapshotId))
        {
            snapshotId = previousSnapshotId + 1;

            previousBlobReferences = GetSnapshot(previousSnapshotId)
                .BlobReferences
                .ToDictionary(br => br.Blob, br => br);
        }

        var blobReferences = blobSystem.ListBlobs(regex)
            .Select(b => previousBlobReferences.TryGetValue(b, out var br)
                ? br
                : StoreBlob(blobSystem, b))
            .ToArray();

        var snapshot = new Snapshot(
            blobReferences.Max(br => br.Blob.LastWriteTimeUtc),
            blobReferences);

        StoreSnapshotReference(snapshotId, snapshot);

        return snapshotId;
    }

    public Snapshot GetSnapshot(int snapshotId)
    {
        return GetSnapshot(
            GetSnapshotReference(snapshotId));
    }

    public bool CheckSnapshot(int snapshotId, Regex? regex = null)
    {
        var snapshotValid = GetSnapshot(snapshotId).ListBlobReferences(regex)
            .CheckAll(CheckBlobReference);

        return snapshotValid;
    }

    public void RestoreSnapshot(
        IBlobSystem blobSystem,
        int snapshotId,
        Regex? regex = null)
    {
        var blobReferencesToRestore = GetSnapshot(snapshotId)
            .ListBlobReferences(regex)
            .Where(br => !br.Blob.Equals(blobSystem.GetBlob(br.Blob.Name)));

        foreach (var blobReference in blobReferencesToRestore)
        {
            RestoreBlob(blobSystem, blobReference);
        }
    }

    public int[] ListSnapshotIds()
    {
        var snapshotIds = _repository.Snapshots.UnorderedList();

        Array.Sort(snapshotIds);

        return snapshotIds;
    }

    public void GarbageCollect()
    {
        var usedChunkIds = ListUsedChunkIds();

        foreach (var chunkId in _repository.Chunks.UnorderedList())
        {
            if (!usedChunkIds.Contains(chunkId))
            {
                _repository.Chunks.Remove(chunkId);
            }
        }
    }

    public void RemoveSnapshot(int snapshotId)
    {
        _repository.Snapshots.Remove(snapshotId);
    }

    private void StoreSnapshotReference(int snapshotId, Snapshot snapshot)
    {
        var snapshotReference = new SnapshotReference(
            _chunker.Value.Salt,
            _chunker.Value.Iterations,
            _chunker.Value.StoreChunks(
                Serializer.SnapshotToBytes(snapshot)));

        _repository.Snapshots.Write(
            snapshotId,
            Serializer.SnapshotReferenceToBytes(snapshotReference));
    }

    private SnapshotReference GetSnapshotReference(int snapshotId)
    {
        return Serializer.BytesToSnapshotReference(
            _repository.Snapshots.Retrieve(snapshotId));
    }

    private Snapshot GetSnapshot(SnapshotReference snapshotReference)
    {
        return Serializer.BytesToSnapshot(
            _chunker.Value.RestoreChunks(snapshotReference.ChunkIds));
    }

    private BlobReference StoreBlob(IBlobSystem blobSystem, Blob blob)
    {
        using var stream = blobSystem.OpenRead(blob.Name);

        var blobReference = new BlobReference(
            blob,
            _chunker.Value.StoreChunks(stream));

        _probe.StoredBlob(blobReference.Blob);

        return blobReference;
    }

    private bool CheckBlobReference(BlobReference blobReference)
    {
        var blobValid = blobReference.ChunkIds.CheckAll(
            _chunker.Value.CheckChunk);

        _probe.ValidatedBlob(blobReference.Blob, blobValid);

        return blobValid;
    }

    private void RestoreBlob(
        IBlobSystem blobSystem,
        BlobReference blobReference)
    {
        using (var stream = blobSystem.OpenWrite(blobReference.Blob))
        {
            _chunker.Value.RestoreChunks(blobReference.ChunkIds, stream);
        }

        _probe.RestoredBlob(blobReference.Blob);
    }

    private HashSet<string> ListUsedChunkIds()
    {
        var chunkIds = new HashSet<string>();

        foreach (var snapshotId in _repository.Snapshots.UnorderedList())
        {
            var snapshotReference = GetSnapshotReference(snapshotId);

            var blobChunkIds = GetSnapshot(snapshotReference).BlobReferences
                .SelectMany(br => br.ChunkIds);

            chunkIds.UnionWith(snapshotReference.ChunkIds);
            chunkIds.UnionWith(blobChunkIds);
        }

        return chunkIds;
    }

    private bool TryLastSnapshotId(out int key)
    {
        var keys = _repository.Snapshots.UnorderedList();
        var any = keys.Length > 0;

        key = any
            ? keys.Max()
            : 0;

        return any;
    }
}
