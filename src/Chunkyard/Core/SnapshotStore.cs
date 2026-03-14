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
        var blobReferences = StoreBlobs(blobSystem, regex);

        var snapshot = new Snapshot(
            blobReferences.Max(br => br.Blob.LastWriteTimeUtc),
            blobReferences);

        var snapshotReference = new SnapshotReference(
            _chunker.Value.Salt,
            _chunker.Value.Iterations,
            StoreSnapshot(snapshot));

        var snapshotId = StoreSnapshotReference(snapshotReference);

        _probe.StoredSnapshot(snapshotId);

        return snapshotId;
    }

    public SnapshotReference GetSnapshotReference(int snapshotId)
    {
        snapshotId = ResolveSnapshotId(snapshotId);
        var bytes = _repository.Snapshots.Retrieve(snapshotId);

        try
        {
            return Serializer.BytesToSnapshotReference(bytes);
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not retrieve snapshot: #{snapshotId}",
                e);
        }
    }

    public Snapshot GetSnapshot(SnapshotReference snapshotReference)
    {
        using var memoryStream = new MemoryStream();

        _chunker.Value.RestoreChunks(snapshotReference.ChunkIds, memoryStream);

        return Serializer.BytesToSnapshot(
            memoryStream.GetBuffer().AsSpan(0, (int)memoryStream.Length));
    }

    public Snapshot GetSnapshot(int snapshotId)
    {
        return GetSnapshot(
            GetSnapshotReference(snapshotId));
    }

    public bool CheckSnapshot(int snapshotId, Regex? regex = null)
    {
        snapshotId = ResolveSnapshotId(snapshotId);

        var snapshotValid = GetSnapshot(snapshotId).ListBlobReferences(regex)
            .CheckAll(CheckBlobReference);

        _probe.SnapshotValid(snapshotId, snapshotValid);

        return snapshotValid;
    }

    public void RestoreSnapshot(
        IBlobSystem blobSystem,
        int snapshotId,
        Regex? regex = null)
    {
        snapshotId = ResolveSnapshotId(snapshotId);

        var blobReferencesToRestore = GetSnapshot(snapshotId)
            .ListBlobReferences(regex)
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
        var usedChunkIds = ListChunkIds(_repository.Snapshots.UnorderedList());

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
        snapshotId = ResolveSnapshotId(snapshotId);

        _repository.Snapshots.Remove(snapshotId);
        _probe.RemovedSnapshot(snapshotId);
    }

    public void CopyTo(IRepository otherRepository)
    {
        var chunkIds = _repository.Chunks.UnorderedList()
            .Except(otherRepository.Chunks.UnorderedList());

        foreach (var chunkId in chunkIds)
        {
            otherRepository.Chunks.Write(
                chunkId,
                _repository.Chunks.Retrieve(chunkId));
        }

        var snapshotIds = _repository.Snapshots.UnorderedList()
            .Except(otherRepository.Snapshots.UnorderedList());

        foreach (var snapshotId in snapshotIds)
        {
            otherRepository.Snapshots.Write(
                snapshotId,
                _repository.Snapshots.Retrieve(snapshotId));
        }
    }

    private BlobReference[] StoreBlobs(
        IBlobSystem blobSystem,
        Regex? regex)
    {
        var existingBlobReferences = TryLastSnapshotId(out var snapshotId)
            ? GetSnapshot(snapshotId).BlobReferences.ToDictionary(br => br.Blob, br => br)
            : new Dictionary<Blob, BlobReference>();

        var blobs = blobSystem.ListBlobs(regex);

        return blobs
            .Select(b => existingBlobReferences.TryGetValue(b, out var br)
                ? br
                : StoreBlob(blobSystem, b))
            .OrderBy(br => br.Blob.Name)
            .ToArray();
    }

    private string[] StoreSnapshot(Snapshot snapshot)
    {
        using var memoryStream = new MemoryStream(
            Serializer.SnapshotToBytes(snapshot));

        return _chunker.Value.StoreChunks(memoryStream);
    }

    private int StoreSnapshotReference(SnapshotReference snapshotReference)
    {
        var nextId = TryLastSnapshotId(out var snapshotId)
            ? snapshotId + 1
            : 0;

        _repository.Snapshots.Write(
            nextId,
            Serializer.SnapshotReferenceToBytes(snapshotReference));

        return nextId;
    }

    private bool CheckBlobReference(BlobReference blobReference)
    {
        var blobValid = blobReference.ChunkIds.CheckAll(_chunker.Value.CheckChunk);

        _probe.BlobValid(blobReference.Blob, blobValid);

        return blobValid;
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

        var snapshotIds = ListSnapshotIds();
        var position = snapshotIds.Length + snapshotId;

        return snapshotIds[position];
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
