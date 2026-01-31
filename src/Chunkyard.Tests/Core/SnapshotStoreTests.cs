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
    public void GetSnapshot_Accepts_Negative_SnapshotIds_With_Gaps()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        var snapshotId1 = snapshotStore.StoreSnapshot(blobSystem);
        var snapshotId2 = snapshotStore.StoreSnapshot(blobSystem);
        var snapshotId3 = snapshotStore.StoreSnapshot(blobSystem);
        snapshotStore.RemoveSnapshot(snapshotId2);

        Assert.AreEqual(
            snapshotStore.GetSnapshot(snapshotId1),
            snapshotStore.GetSnapshot(SnapshotStore.SecondLatestSnapshotId));

        Assert.AreEqual(
            snapshotStore.GetSnapshot(snapshotId3),
            snapshotStore.GetSnapshot(SnapshotStore.LatestSnapshotId));
    }

    [TestMethod]
    public void GetSnapshot_Throws_When_Accessing_Out_Of_Bounds_Negative_SnapshotId()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        _ = snapshotStore.StoreSnapshot(blobSystem);

        _ = Assert.Throws<IndexOutOfRangeException>(
            () => snapshotStore.GetSnapshot(
                SnapshotStore.SecondLatestSnapshotId));
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

        _ = Assert.Throws<ChunkyardException>(
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

        _ = Assert.Throws<ChunkyardException>(
            () => snapshotStore.CheckSnapshot(snapshotId));
    }

    [TestMethod]
    public void CheckSnapshot_Detects_Missing_Blob()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        var snapshotReference = snapshotStore.GetSnapshotReference(snapshotId);

        var chunkIds = snapshotStore.GetSnapshot(snapshotReference)
            .BlobReferences
            .SelectMany(b => b.ChunkIds)
            .Except(snapshotReference.ChunkIds);

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

        var snapshotReference = snapshotStore.GetSnapshotReference(snapshotId);

        var chunkIds = snapshotStore.GetSnapshot(snapshotReference)
            .BlobReferences
            .SelectMany(b => b.ChunkIds)
            .Except(snapshotReference.ChunkIds);

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
    public void GarbageCollect_Removes_Unused_Ids()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        snapshotStore.GarbageCollect();

        Assert.IsTrue(snapshotStore.CheckSnapshot(snapshotId));

        snapshotStore.RemoveSnapshot(snapshotId);

        Assert.IsNotEmpty(repository.Chunks.UnorderedList());

        snapshotStore.GarbageCollect();

        Assert.IsEmpty(repository.Snapshots.UnorderedList());
    }

    [TestMethod]
    public void RemoveSnapshot_Removes_Existing_Snapshots()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);
        _ = snapshotStore.StoreSnapshot(blobSystem);

        snapshotStore.RemoveSnapshot(snapshotId);
        snapshotStore.RemoveSnapshot(SnapshotStore.LatestSnapshotId);

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

    [TestMethod]
    public void CopyTo_Copies_Newer_Snapshots()
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

        CollectionAssert.AreEquivalent(
            repository.Chunks.UnorderedList(),
            otherRepository.Chunks.UnorderedList());

        CollectionAssert.AreEquivalent(
            repository.Snapshots.UnorderedList(),
            otherRepository.Snapshots.UnorderedList());
    }

    [TestMethod]
    public void CopyTo_Does_Nothing_If_Behind()
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

        CollectionAssert.AreEquivalent(
            expectedChunks,
            otherRepository.Chunks.UnorderedList());

        CollectionAssert.AreEquivalent(
            expectedSnapshots,
            otherRepository.Snapshots.UnorderedList());
    }

    [TestMethod]
    public void CopyTo_Throws_On_Shared_SnapshotReference_Mismatch()
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
}
