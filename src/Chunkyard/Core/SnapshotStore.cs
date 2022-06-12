namespace Chunkyard.Core;

/// <summary>
/// A class which uses a <see cref="ChunkStore"/> to store snapshots of a set
/// of blobs.
/// </summary>
public class SnapshotStore
{
    private readonly ChunkStore _chunkStore;
    private readonly IProbe _probe;
    private readonly IClock _clock;

    public SnapshotStore(
        ChunkStore chunkStore,
        IProbe probe,
        IClock clock)
    {
        _chunkStore = chunkStore;
        _probe = probe;
        _clock = clock;
    }

    public DiffSet StoreSnapshotPreview(IBlobSystem blobSystem)
    {
        ArgumentNullException.ThrowIfNull(blobSystem);

        var blobReferences = _chunkStore.CurrentLogId == null
            ? Array.Empty<BlobReference>()
            : GetSnapshot(_chunkStore.CurrentLogId.Value).BlobReferences;

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

        var logId = _chunkStore.WriteLog(
            WriteSnapshot(newSnapshot));

        _probe.StoredSnapshot(logId);

        return logId;
    }

    public bool CheckSnapshotExists(int snapshotId, Fuzzy includeFuzzy)
    {
        return CheckSnapshot(
            snapshotId,
            includeFuzzy,
            _chunkStore.ChunkExists);
    }

    public bool CheckSnapshotValid(int snapshotId, Fuzzy includeFuzzy)
    {
        return CheckSnapshot(
            snapshotId,
            includeFuzzy,
            _chunkStore.ChunkValid);
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
            _chunkStore.ResolveLogId(snapshotId));
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

        _ = snapshot.BlobReferences
            .AsParallel()
            .Select(br => MirrorBlob(blobSystem, br))
            .ToArray();

        _probe.RestoredSnapshot(
            _chunkStore.ResolveLogId(snapshotId));

        var blobNamesToRemove = blobSystem.ListBlobs()
            .Select(blob => blob.Name)
            .Except(snapshot.BlobReferences.Select(br => br.Blob.Name));

        foreach (var blobName in blobNamesToRemove)
        {
            blobSystem.RemoveBlob(blobName);
            _probe.RemovedBlob(blobName);
        }
    }

    public Snapshot GetSnapshot(int snapshotId)
    {
        return GetSnapshot(
            _chunkStore.ListChunkIds(snapshotId));
    }

    public IReadOnlyCollection<int> ListSnapshotIds()
    {
        return _chunkStore.ListLogIds();
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
        var usedChunkIds = ListChunkIds(_chunkStore.ListLogIds());
        var unusedChunkIds = _chunkStore.ListChunkIds()
            .Except(usedChunkIds);

        foreach (var chunkId in unusedChunkIds)
        {
            _chunkStore.RemoveChunk(chunkId);
            _probe.RemovedChunk(chunkId);
        }
    }

    public void RemoveSnapshot(int logId)
    {
        var removedLogId = _chunkStore.RemoveLog(logId);

        _probe.RemovedSnapshot(removedLogId);
    }

    public void KeepSnapshots(int latestCount)
    {
        var snapshotIds = _chunkStore.ListLogIds();
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
        _chunkStore.RestoreChunks(chunkIds, outputStream);
    }

    public void CopyTo(IRepository otherRepository)
    {
        ArgumentNullException.ThrowIfNull(otherRepository);

        var localSnapshotIds = _chunkStore.ListLogIds();
        var otherSnapshotIds = otherRepository.Log.ListKeys();

        var otherSnapshotIdMax = otherSnapshotIds.Count == 0
            ? ChunkStore.LatestLogId
            : otherSnapshotIds.Max();

        var snapshotIdsToCopy = localSnapshotIds
            .Where(id => id > otherSnapshotIdMax)
            .ToArray();

        var chunkIdsToCopy = ListChunkIds(snapshotIdsToCopy)
            .Except(otherRepository.Chunks.ListKeys())
            .ToArray();

        foreach (var chunkId in chunkIdsToCopy)
        {
            _chunkStore.CopyChunk(otherRepository, chunkId);
            _probe.CopiedChunk(chunkId);
        }

        foreach (var snapshotId in snapshotIdsToCopy)
        {
            _chunkStore.CopyLog(otherRepository, snapshotId);
            _probe.CopiedSnapshot(snapshotId);
        }
    }

    private IReadOnlyCollection<string> ListChunkIds(IEnumerable<int> snapshotIds)
    {
        var chunkIds = new HashSet<string>();

        foreach (var snapshotId in snapshotIds)
        {
            var logChunkIds = _chunkStore.ListChunkIds(snapshotId);
            var blobChunkIds = GetSnapshot(logChunkIds).BlobReferences
                .SelectMany(br => br.ChunkIds);

            chunkIds.UnionWith(logChunkIds);
            chunkIds.UnionWith(blobChunkIds);
        }

        return chunkIds;
    }

    private Snapshot GetSnapshot(IReadOnlyCollection<string> chunkIds)
    {
        using var memoryStream = new MemoryStream();

        try
        {
            _chunkStore.RestoreChunks(chunkIds, memoryStream);

            return Serialize.BytesToSnapshot(memoryStream.ToArray());
        }
        catch (Exception e)
        {
            var chunkIdsText = string.Join(
                ' ',
                chunkIds.Select(ChunkId.Shorten));

            throw new ChunkyardException(
                $"Could not read snapshot: {chunkIdsText}",
                e);
        }
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
            _chunkStore.ResolveLogId(snapshotId),
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
            _chunkStore.RestoreChunks(blobReference.ChunkIds, stream);
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
        var currentBlobReferences = _chunkStore.CurrentLogId == null
            ? new Dictionary<string, BlobReference>()
            : GetSnapshot(_chunkStore.CurrentLogId.Value).BlobReferences
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
                _chunkStore.WriteChunks(nonce, stream));

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

        return _chunkStore.WriteChunks(
            Crypto.GenerateNonce(),
            memoryStream);
    }
}
