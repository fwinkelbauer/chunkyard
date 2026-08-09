namespace Chunkyard.Tests.Core;

[TestClass]
public sealed class SnapshotStoreTests
{
    [TestMethod]
    public void StoreSnapshot_Creates_Snapshot_Of_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);
        var snapshot = snapshotStore.GetSnapshot(snapshotId);

        CollectionAssert.AreEqual(
            blobSystem.ListBlobs(),
            snapshot.BlobReferences.Select(br => br.Blob).ToArray());
    }

    [TestMethod]
    public void StoreSnapshot_Skips_Blobs_With_Unchanged_LastWriteTimeUtc()
    {
        var snapshotStore = Some.SnapshotStore();
        var sharedBlob = Some.Blob("some blob");

        var blobSystem1 = Some.BlobSystem(
            new[] { sharedBlob });

        var blobSystem2 = Some.BlobSystem(
            new[] { sharedBlob, Some.Blob("other blob") });

        var snapshotId1 = snapshotStore.StoreSnapshot(blobSystem1);
        var snapshotId2 = snapshotStore.StoreSnapshot(blobSystem2);

        var snapshot1 = snapshotStore.GetSnapshot(snapshotId1);
        var snapshot2 = snapshotStore.GetSnapshot(snapshotId2);

        CollectionAssert.AreEqual(
            snapshot1.BlobReferences.Where(br => Equals(br.Blob, sharedBlob)).ToArray(),
            snapshot2.BlobReferences.Where(br => Equals(br.Blob, sharedBlob)).ToArray());
    }

    [TestMethod]
    public void CheckSnapshot_Detects_Valid_Snapshot()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Assert.IsTrue(
            snapshotStore.CheckSnapshot(snapshotId));
    }

    [TestMethod]
    public void CheckSnapshot_Throws_If_Missing_Snapshot()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        repository.Chunks.Missing(repository.Chunks.UnorderedList());

        _ = Assert.Throws<Exception>(
            () => snapshotStore.CheckSnapshot(snapshotId));
    }

    [TestMethod]
    public void CheckSnapshot_Throws_If_Corrupt_Snapshot()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        repository.Chunks.Corrupt(repository.Chunks.UnorderedList());

        _ = Assert.Throws<Exception>(
            () => snapshotStore.CheckSnapshot(snapshotId));
    }

    [TestMethod]
    public void CheckSnapshot_Detects_Missing_Blob()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        var chunkIds = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences
            .SelectMany(b => b.ChunkIds);

        repository.Chunks.Missing(chunkIds);

        Assert.IsFalse(
            snapshotStore.CheckSnapshot(snapshotId));
    }

    [TestMethod]
    public void CheckSnapshot_Detects_Corrupt_Blob()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        var chunkIds = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences
            .SelectMany(b => b.ChunkIds);

        repository.Chunks.Corrupt(chunkIds);

        Assert.IsFalse(
            snapshotStore.CheckSnapshot(snapshotId));
    }

    [TestMethod]
    public void RestoreSnapshot_Overwrites_Known_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();

        var inputBlobSystem = Some.BlobSystem(
            Some.Blobs("blob to restore", "blob to update"));

        var outputBlobSystem = Some.BlobSystem(
            Some.Blobs("blob to update"));

        var snapshotId = snapshotStore.StoreSnapshot(inputBlobSystem);
        snapshotStore.RestoreSnapshot(outputBlobSystem, snapshotId);

        CollectionAssert.AreEqual(
            inputBlobSystem.ToDictionary(),
            outputBlobSystem.ToDictionary());
    }

    [TestMethod]
    public void RestoreSnapshot_Can_Write_Empty_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();
        var inputBlobSystem = Some.BlobSystem(Some.Blobs(), []);
        var outputBlobSystem = Some.BlobSystem();

        var snapshotId = snapshotStore.StoreSnapshot(inputBlobSystem);
        snapshotStore.RestoreSnapshot(outputBlobSystem, snapshotId);

        CollectionAssert.AreEqual(
            inputBlobSystem.ToDictionary(),
            outputBlobSystem.ToDictionary());
    }

    [TestMethod]
    public void GarbageCollect_Removes_Unused_Data()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId1 = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs("a")));

        var snapshotId2 = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs("b")));

        snapshotStore.GarbageCollect();

        Assert.IsTrue(snapshotStore.CheckSnapshot(snapshotId1));
        Assert.IsTrue(snapshotStore.CheckSnapshot(snapshotId2));

        snapshotStore.RemoveSnapshot(snapshotId1);
        snapshotStore.GarbageCollect();

        Assert.IsTrue(snapshotStore.CheckSnapshot(snapshotId2));

        snapshotStore.RemoveSnapshot(snapshotId2);
        snapshotStore.GarbageCollect();

        Assert.IsEmpty(repository.Chunks.UnorderedList());
        Assert.IsEmpty(repository.Snapshots.UnorderedList());
    }

    [TestMethod]
    public void RemoveSnapshot_Removes_Snapshot()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        snapshotStore.RemoveSnapshot(
            snapshotStore.StoreSnapshot(blobSystem));

        Assert.IsEmpty(snapshotStore.ListSnapshotIds());
    }

    [TestMethod]
    public void ListSnapshotIds_Lists_Sorted_Ids()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        var snapshotIds = new[]
        {
            snapshotStore.StoreSnapshot(blobSystem),
            snapshotStore.StoreSnapshot(blobSystem),
            snapshotStore.StoreSnapshot(blobSystem)
        };

        CollectionAssert.AreEqual(
            snapshotIds,
            snapshotStore.ListSnapshotIds());
    }
}
