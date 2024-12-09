namespace Chunkyard.Tests.Core;

public static class SnapshotStoreTests
{
    [Fact]
    public static void StoreSnapshot_Creates_Snapshot_Of_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);
        var snapshot = snapshotStore.GetSnapshot(snapshotId);

        Assert.Equal(
            blobSystem.ListBlobs(),
            snapshot.BlobReferences.Select(br => br.Blob));
    }

    [Fact]
    public static void StoreSnapshot_Skips_Blobs_With_Unchanged_LastWriteTimeUtc()
    {
        var snapshotStore = Some.SnapshotStore();
        var sharedBlob = Some.Blob("some blob");

        var blobSystem1 = Some.BlobSystem(
            new[] { sharedBlob },
            _ => new byte[] { 0x01 });

        var blobSystem2 = Some.BlobSystem(
            new[] { sharedBlob, Some.Blob("other blob") },
            _ => new byte[] { 0x02 });

        var snapshotId1 = snapshotStore.StoreSnapshot(blobSystem1);
        var snapshotId2 = snapshotStore.StoreSnapshot(blobSystem2);

        var snapshot1 = snapshotStore.GetSnapshot(snapshotId1);
        var snapshot2 = snapshotStore.GetSnapshot(snapshotId2);

        Assert.Equal(
            snapshot1.BlobReferences.Where(br => Equals(br.Blob, sharedBlob)),
            snapshot2.BlobReferences.Where(br => Equals(br.Blob, sharedBlob)));
    }

    [Fact]
    public static void GetSnapshot_Accepts_Negative_SnapshotIds_With_Gaps()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        var snapshotId1 = snapshotStore.StoreSnapshot(blobSystem);
        var snapshotId2 = snapshotStore.StoreSnapshot(blobSystem);
        var snapshotId3 = snapshotStore.StoreSnapshot(blobSystem);
        snapshotStore.RemoveSnapshot(snapshotId2);

        Assert.Equal(
            snapshotStore.GetSnapshot(snapshotId1),
            snapshotStore.GetSnapshot(SnapshotStore.SecondLatestSnapshotId));

        Assert.Equal(
            snapshotStore.GetSnapshot(snapshotId3),
            snapshotStore.GetSnapshot(SnapshotStore.LatestSnapshotId));
    }

    [Fact]
    public static void CheckSnapshot_Detects_Valid_Snapshot()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Assert.True(
            snapshotStore.CheckSnapshotExists(snapshotId));

        Assert.True(
            snapshotStore.CheckSnapshotValid(snapshotId));
    }

    [Fact]
    public static void CheckSnapshot_Throws_If_Missing_Snapshot()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Missing(repository.Chunks, repository.Chunks.UnorderedList());

        _ = Assert.Throws<ChunkyardException>(
            () => snapshotStore.CheckSnapshotExists(snapshotId));

        _ = Assert.Throws<ChunkyardException>(
            () => snapshotStore.CheckSnapshotValid(snapshotId));
    }

    [Fact]
    public static void CheckSnapshot_Throws_If_Corrupt_Snapshot()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Corrupt(repository.Chunks, repository.Chunks.UnorderedList());

        _ = Assert.Throws<ChunkyardException>(
            () => snapshotStore.CheckSnapshotExists(snapshotId));

        _ = Assert.Throws<ChunkyardException>(
            () => snapshotStore.CheckSnapshotValid(snapshotId));
    }

    [Fact]
    public static void CheckSnapshot_Detects_Missing_Blob()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        var chunkIds = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences
            .SelectMany(b => b.ChunkIds);

        Missing(repository.Chunks, chunkIds);

        Assert.False(
            snapshotStore.CheckSnapshotExists(snapshotId));

        Assert.False(
            snapshotStore.CheckSnapshotValid(snapshotId));
    }

    [Fact]
    public static void CheckSnapshot_Detects_Corrupt_Blob()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        var chunkIds = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences
            .SelectMany(b => b.ChunkIds);

        Corrupt(repository.Chunks, chunkIds);

        Assert.True(
            snapshotStore.CheckSnapshotExists(snapshotId));

        Assert.False(
            snapshotStore.CheckSnapshotValid(snapshotId));
    }

    [Fact]
    public static void RestoreSnapshot_Updates_Known_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();

        var inputBlobSystem = Some.BlobSystem(
            Some.Blobs("blob to restore", "blob to update"),
            _ => new byte[] { 0x01 });

        var outputBlobSystem = Some.BlobSystem(
            Some.Blobs("blob to update"),
            _ => new byte[] { 0x01, 0x02 });

        var snapshotId = snapshotStore.StoreSnapshot(inputBlobSystem);
        snapshotStore.RestoreSnapshot(outputBlobSystem, snapshotId);

        Assert.Equal(
            ToDictionary(inputBlobSystem),
            ToDictionary(outputBlobSystem));
    }

    [Fact]
    public static void RestoreSnapshot_Can_Write_Empty_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();
        var inputBlobSystem = Some.BlobSystem(Some.Blobs(), _ => []);
        var outputBlobSystem = Some.BlobSystem();

        var snapshotId = snapshotStore.StoreSnapshot(inputBlobSystem);
        snapshotStore.RestoreSnapshot(outputBlobSystem, snapshotId);

        Assert.Equal(
            ToDictionary(inputBlobSystem),
            ToDictionary(outputBlobSystem));
    }

    [Fact]
    public static void RestoreSnapshot_Keeps_Chunks_Order()
    {
        var fastCdc = new FastCdc(256, 512, 1024);
        var snapshotStore = Some.SnapshotStore(fastCdc: fastCdc);

        // Generate data that is large enough to create a few chunks
        var inputBlobSystem = Some.BlobSystem(
            Some.Blobs(),
            _ => RandomNumberGenerator.GetBytes(8 * fastCdc.MaxSize));

        var snapshotId = snapshotStore.StoreSnapshot(inputBlobSystem);
        var outputBlobSystem = Some.BlobSystem();
        snapshotStore.RestoreSnapshot(outputBlobSystem, snapshotId);

        Assert.Equal(
            ToDictionary(inputBlobSystem),
            ToDictionary(outputBlobSystem));

        var blobReferences = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences;

        var chunkIds = blobReferences.SelectMany(br => br.ChunkIds).ToArray();

        Assert.NotEmpty(blobReferences);
        Assert.True(blobReferences.Count * 2 <= chunkIds.Length);
    }

    [Fact]
    public static void GarbageCollect_Removes_Unused_Ids()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        snapshotStore.GarbageCollect();

        Assert.True(snapshotStore.CheckSnapshotValid(snapshotId));

        snapshotStore.RemoveSnapshot(snapshotId);

        Assert.NotEmpty(repository.Chunks.UnorderedList());

        snapshotStore.GarbageCollect();

        Assert.Empty(repository.Chunks.UnorderedList());
        Assert.Empty(repository.Snapshots.UnorderedList());
    }

    [Fact]
    public static void RemoveSnapshot_Removes_Existing_Snapshots()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);
        _ = snapshotStore.StoreSnapshot(blobSystem);

        snapshotStore.RemoveSnapshot(snapshotId);
        snapshotStore.RemoveSnapshot(SnapshotStore.LatestSnapshotId);

        Assert.Empty(snapshotStore.ListSnapshotIds());
    }

    [Fact]
    public static void ListSnapshotIds_Lists_Sorted_Ids()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        var snapshotIds = new[]
        {
            snapshotStore.StoreSnapshot(blobSystem),
            snapshotStore.StoreSnapshot(blobSystem),
            snapshotStore.StoreSnapshot(blobSystem)
        };

        Assert.Equal(
            snapshotIds,
            snapshotStore.ListSnapshotIds());
    }

    [Fact]
    public static void KeepSnapshots_Removes_Older_Snapshots()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        _ = snapshotStore.StoreSnapshot(blobSystem);
        _ = snapshotStore.StoreSnapshot(blobSystem);

        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);

        snapshotStore.KeepSnapshots(1);
        snapshotStore.KeepSnapshots(2);

        Assert.Equal(
            new[] { snapshotId },
            snapshotStore.ListSnapshotIds());

        snapshotStore.KeepSnapshots(0);

        Assert.Empty(snapshotStore.ListSnapshotIds());
    }

    [Fact]
    public static void CopyTo_Copies_Newer_Snapshots()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);
        var otherRepository = Some.Repository();

        _ = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs("some blob")));

        _ = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs("some blob", "other blob")));

        snapshotStore.CopyTo(otherRepository);

        _ = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs("different blob")));

        snapshotStore.CopyTo(otherRepository);

        Assert.Equivalent(
            repository.Chunks.UnorderedList(),
            otherRepository.Chunks.UnorderedList(),
            true);

        Assert.Equivalent(
            repository.Snapshots.UnorderedList(),
            otherRepository.Snapshots.UnorderedList(),
            true);
    }

    [Fact]
    public static void CopyTo_Does_Nothing_If_Behind()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);
        var otherRepository = Some.Repository();
        var otherSnapshotStore = Some.SnapshotStore(otherRepository);

        _ = otherSnapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs("some blob")));

        var expectedChunks = otherRepository.Chunks.UnorderedList();
        var expectedSnapshots = otherRepository.Snapshots.UnorderedList();

        snapshotStore.CopyTo(otherRepository);

        Assert.Equivalent(
            expectedChunks,
            otherRepository.Chunks.UnorderedList(),
            true);

        Assert.Equivalent(
            expectedSnapshots,
            otherRepository.Snapshots.UnorderedList(),
            true);
    }

    [Fact]
    public static void CopyTo_Can_Limit_Copied_Snapshots()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());
        var otherRepository = Some.Repository();
        var otherSnapshotStore = Some.SnapshotStore(otherRepository);

        _ = snapshotStore.StoreSnapshot(blobSystem);
        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);
        snapshotStore.CopyTo(otherRepository, 1);

        Assert.Equal(
            new[] { snapshotId },
            otherSnapshotStore.ListSnapshotIds());

        Assert.True(otherSnapshotStore.CheckSnapshotValid(snapshotId));
    }

    [Fact]
    public static void CopyTo_Throws_On_Corrupt_References()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        _ = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Corrupt(repository.Snapshots, repository.Snapshots.UnorderedList());

        _ = Assert.Throws<ChunkyardException>(
            () => snapshotStore.CopyTo(Some.Repository()));
    }

    [Fact]
    public static void CopyTo_Throws_On_Shared_SnapshotReference_Mismatch()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var otherRepository = Some.Repository();
        var otherSnapshotStore = Some.SnapshotStore(otherRepository);

        _ = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs("some blob")));

        _ = otherSnapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs("other blob")));

        _ = Assert.Throws<ChunkyardException>(
            () => snapshotStore.CopyTo(otherRepository));
    }

    private static void Missing<T>(
        IRepository<T> repository,
        IEnumerable<T> keys)
    {
        foreach (var key in keys)
        {
            repository.Remove(key);
        }
    }

    private static void Corrupt<T>(
        IRepository<T> repository,
        IEnumerable<T> keys)
    {
        foreach (var key in keys)
        {
            var bytes = repository.Retrieve(key);

            repository.Remove(key);
            repository.Store(
                key,
                bytes.Concat(new byte[] { 0xFF }).ToArray());
        }
    }

    private static Dictionary<Blob, string> ToDictionary(
        IBlobSystem blobSystem)
    {
        return blobSystem.ListBlobs().ToDictionary(
            blob => blob,
            blob =>
            {
                using var memoryStream = new MemoryStream();
                using var blobStream = blobSystem.OpenRead(blob.Name);

                blobStream.CopyTo(memoryStream);

                return Convert.ToHexString(memoryStream.ToArray());
            });
    }
}
