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
            new[] { sharedBlob },
            _ => new byte[] { 0x01 });

        var blobSystem2 = Some.BlobSystem(
            new[] { sharedBlob, Some.Blob("other blob") },
            _ => new byte[] { 0x02 });

        var snapshotId1 = snapshotStore.StoreSnapshot(blobSystem1);
        var snapshotId2 = snapshotStore.StoreSnapshot(blobSystem2);

        var snapshot1 = snapshotStore.GetSnapshot(snapshotId1);
        var snapshot2 = snapshotStore.GetSnapshot(snapshotId2);

        CollectionAssert.AreEqual(
            blobSystem1.ListBlobs(),
            snapshot1.BlobReferences.Select(br => br.Blob).ToArray());

        CollectionAssert.AreEqual(
            blobSystem2.ListBlobs(),
            snapshot2.BlobReferences.Select(br => br.Blob).ToArray());

        CollectionAssert.AreEqual(
            snapshot1.BlobReferences.Where(br => br.Blob == sharedBlob).ToArray(),
            snapshot2.BlobReferences.Where(br => br.Blob == sharedBlob).ToArray());
    }

    [TestMethod]
    public void StoreSnapshotPreview_Shows_Preview()
    {
        var snapshotStore = Some.SnapshotStore();
        var initialBlobs = Some.Blobs("some blob");
        var initialBlobSystem = Some.BlobSystem(initialBlobs);
        var changedBlobs = Some.Blobs("other blob");
        var changedBlobSystem = Some.BlobSystem(changedBlobs);

        var initialDiff = snapshotStore.StoreSnapshotPreview(initialBlobSystem);
        _ = snapshotStore.StoreSnapshot(initialBlobSystem);
        var changedDiff = snapshotStore.StoreSnapshotPreview(changedBlobSystem);

        Assert.AreEqual(
            new DiffSet<Blob>(
                initialBlobs,
                Array.Empty<Blob>(),
                Array.Empty<Blob>()),
            initialDiff);

        Assert.AreEqual(
            new DiffSet<Blob>(
                changedBlobs,
                Array.Empty<Blob>(),
                initialBlobs),
            changedDiff);
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
    public void CheckSnapshot_Detects_Valid_Snapshot()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Assert.IsTrue(
            snapshotStore.CheckSnapshotExists(snapshotId));

        Assert.IsTrue(
            snapshotStore.CheckSnapshotValid(snapshotId));
    }

    [TestMethod]
    public void CheckSnapshot_Throws_If_Missing_Snapshot()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Missing(repository.Chunks, repository.Chunks.UnorderedList());

        Assert.ThrowsException<ChunkyardException>(
            () => snapshotStore.CheckSnapshotExists(snapshotId));

        Assert.ThrowsException<ChunkyardException>(
            () => snapshotStore.CheckSnapshotValid(snapshotId));
    }

    [TestMethod]
    public void CheckSnapshot_Throws_If_Corrupt_Snapshot()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Corrupt(repository.Chunks, repository.Chunks.UnorderedList());

        Assert.ThrowsException<ChunkyardException>(
            () => snapshotStore.CheckSnapshotExists(snapshotId));

        Assert.ThrowsException<ChunkyardException>(
            () => snapshotStore.CheckSnapshotValid(snapshotId));
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

        Missing(repository.Chunks, chunkIds);

        Assert.IsFalse(
            snapshotStore.CheckSnapshotExists(snapshotId));

        Assert.IsFalse(
            snapshotStore.CheckSnapshotValid(snapshotId));
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

        Corrupt(repository.Chunks, chunkIds);

        Assert.IsTrue(
           snapshotStore.CheckSnapshotExists(snapshotId));

        Assert.IsFalse(
            snapshotStore.CheckSnapshotValid(snapshotId));
    }

    [TestMethod]
    public void RestoreSnapshot_Updates_Known_Blobs()
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

        CollectionAssert.AreEqual(
            ToDictionary(inputBlobSystem),
            ToDictionary(outputBlobSystem));
    }

    [TestMethod]
    public void RestoreSnapshot_Can_Write_Empty_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();
        var inputBlobSystem = Some.BlobSystem(Some.Blobs(), _ => []);
        var outputBlobSystem = Some.BlobSystem();

        var snapshotId = snapshotStore.StoreSnapshot(inputBlobSystem);
        snapshotStore.RestoreSnapshot(outputBlobSystem, snapshotId);

        CollectionAssert.AreEqual(
            ToDictionary(inputBlobSystem),
            ToDictionary(outputBlobSystem));
    }

    [TestMethod]
    public void RestoreSnapshotPreview_Shows_Preview()
    {
        var blobs = Some.Blobs("some blob");
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(blobs));

        var expected = new DiffSet<Blob>(
            blobs,
            Array.Empty<Blob>(),
            Array.Empty<Blob>());

        var actual = snapshotStore.RestoreSnapshotPreview(
            Some.BlobSystem(),
            snapshotId);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void RestoreSnapshot_Keeps_Chunks_Order()
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

        CollectionAssert.AreEqual(
            ToDictionary(inputBlobSystem),
            ToDictionary(outputBlobSystem));

        var blobReferences = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences
            .ToArray();

        var chunkIds = blobReferences.SelectMany(br => br.ChunkIds).ToArray();

        Assert.IsTrue(blobReferences.Any());
        Assert.IsTrue(blobReferences.Length * 2 <= chunkIds.Length);
    }

    [TestMethod]
    public void GarbageCollect_Removes_Unused_Ids()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        snapshotStore.GarbageCollect();

        Assert.IsTrue(snapshotStore.CheckSnapshotValid(snapshotId));

        snapshotStore.RemoveSnapshot(snapshotId);

        Assert.IsTrue(repository.Chunks.UnorderedList().Any());

        snapshotStore.GarbageCollect();

        Assert.IsFalse(repository.Chunks.UnorderedList().Any());
        Assert.IsFalse(repository.Snapshots.UnorderedList().Any());
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

        Assert.IsFalse(snapshotStore.ListSnapshotIds().Any());
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
    public void KeepSnapshots_Removes_Older_Snapshots()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        _ = snapshotStore.StoreSnapshot(blobSystem);
        _ = snapshotStore.StoreSnapshot(blobSystem);

        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);

        snapshotStore.KeepSnapshots(1);
        snapshotStore.KeepSnapshots(2);

        CollectionAssert.AreEqual(
            new[] { snapshotId },
            snapshotStore.ListSnapshotIds());

        snapshotStore.KeepSnapshots(0);

        Assert.IsFalse(snapshotStore.ListSnapshotIds().Any());
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
    public void CopyTo_Can_Limit_Copied_Snapshots()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());
        var otherRepository = Some.Repository();
        var otherSnapshotStore = Some.SnapshotStore(otherRepository);

        _ = snapshotStore.StoreSnapshot(blobSystem);
        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);
        snapshotStore.CopyTo(otherRepository, 1);

        CollectionAssert.AreEqual(
            new[] { snapshotId },
            otherSnapshotStore.ListSnapshotIds());

        Assert.IsTrue(otherSnapshotStore.CheckSnapshotValid(snapshotId));
    }

    [TestMethod]
    public void CopyTo_Throws_On_Corrupt_References()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        _ = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Corrupt(repository.Snapshots, repository.Snapshots.UnorderedList());

        Assert.ThrowsException<ChunkyardException>(
            () => snapshotStore.CopyTo(Some.Repository()));
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

        Assert.ThrowsException<ChunkyardException>(
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
