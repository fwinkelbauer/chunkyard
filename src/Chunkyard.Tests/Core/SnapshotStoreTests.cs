namespace Chunkyard.Tests.Core;

public static class SnapshotStoreTests
{
    [Fact]
    public static void StoreSnapshot_Stores_Snapshot()
    {
        var snapshotStore = Some.SnapshotStore();
        var expectedBlobs = Some.Blobs();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(expectedBlobs));

        var snapshot = snapshotStore.GetSnapshot(snapshotId);

        Assert.Equal(
            new[] { snapshotId },
            snapshotStore.ListSnapshotIds());

        Assert.Equal(
            expectedBlobs,
            snapshot.BlobReferences.Select(br => br.Blob));
    }

    [Fact]
    public static void StoreSnapshot_Deduplicates_Blobs_Using_Same_Nonce_When_LastWriteTimeUtc_Differs()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobs = Some.Blobs();

        var snapshotId1 = snapshotStore.StoreSnapshot(
            Some.BlobSystem(blobs));

        var snapshotId2 = snapshotStore.StoreSnapshot(
            Some.BlobSystem(
                blobs.Select(b => new Blob(
                    b.Name,
                    b.LastWriteTimeUtc.AddHours(1)))));

        var blobReferences1 = snapshotStore.GetSnapshot(snapshotId1)
            .BlobReferences;

        var blobReferences2 = snapshotStore.GetSnapshot(snapshotId2)
            .BlobReferences;

        Assert.NotEqual(
            blobReferences1,
            blobReferences2);

        Assert.Equal(
            blobReferences1.Select(br => br.Blob.Name),
            blobReferences2.Select(br => br.Blob.Name));

        Assert.Equal(
            blobReferences1.Select(br => br.Nonce),
            blobReferences2.Select(br => br.Nonce));

        Assert.Equal(
            blobReferences1.SelectMany(br => br.ChunkIds),
            blobReferences2.SelectMany(br => br.ChunkIds));
    }

    [Fact]
    public static void StoreSnapshot_Does_Not_Read_Blob_With_Unchanged_LastWriteTimeUtc()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobs = Some.Blobs();

        var blobSystem1 = Some.BlobSystem(
            blobs,
            blobName => new byte[] { 0x11, 0x11 });

        var snapshotId1 = snapshotStore.StoreSnapshot(blobSystem1);

        var blobSystem2 = Some.BlobSystem(
            blobs,
            blobName => new byte[] { 0x22, 0x22 });

        var snapshotId2 = snapshotStore.StoreSnapshot(blobSystem2);

        var snapshot1 = snapshotStore.GetSnapshot(snapshotId1);
        var snapshot2 = snapshotStore.GetSnapshot(snapshotId2);

        Assert.Equal(
            snapshot1.BlobReferences,
            snapshot2.BlobReferences);
    }

    [Fact]
    public static void StoreSnapshot_Stores_Snapshot_Without_Any_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(Some.BlobSystem());
        var snapshot = snapshotStore.GetSnapshot(snapshotId);

        Assert.Empty(snapshot.BlobReferences);
    }

    [Fact]
    public static void StoreSnapshot_Stores_Empty_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(
                Some.Blobs(),
                blobName => Array.Empty<byte>()));

        var chunkIds = snapshotStore.GetSnapshot(snapshotId).BlobReferences
            .SelectMany(br => br.ChunkIds);

        Assert.Empty(chunkIds);
    }

    [Fact]
    public static void New_SnapshotStore_Can_Read_Existing_Snapshot()
    {
        var repository = Some.Repository();
        var expectedBlobs = Some.Blobs();

        var snapshotId = Some.SnapshotStore(repository)
            .StoreSnapshot(Some.BlobSystem(expectedBlobs));

        var actualBlobs = Some.SnapshotStore(repository)
            .GetSnapshot(snapshotId).BlobReferences
            .Select(br => br.Blob);

        Assert.Equal(
            expectedBlobs,
            actualBlobs);
    }

    [Fact]
    public static void GetSnapshot_Accepts_Negative_SnapshotIds()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Assert.Equal(
            snapshotStore.GetSnapshot(snapshotId),
            snapshotStore.GetSnapshot(SnapshotStore.LatestSnapshotId));
    }

    [Fact]
    public static void GetSnapshot_Throws_If_Version_Does_Not_Exist()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.GetSnapshot(snapshotId + 1));

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.GetSnapshot(
                SnapshotStore.SecondLatestSnapshotId));
    }

    [Fact]
    public static void GetSnapshot_Accepts_Negative_SnapshotIds_With_Gaps()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);
        var snapshotIdToRemove = snapshotStore.StoreSnapshot(blobSystem);

        snapshotStore.StoreSnapshot(blobSystem);
        snapshotStore.RemoveSnapshot(snapshotIdToRemove);

        Assert.Equal(
            snapshotStore.GetSnapshot(snapshotId),
            snapshotStore.GetSnapshot(
                SnapshotStore.SecondLatestSnapshotId));
    }

    [Fact]
    public static void GetSnapshot_Throws_When_Empty()
    {
        var snapshotStore = Some.SnapshotStore();

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.GetSnapshot(
                SnapshotStore.LatestSnapshotId));
    }

    [Fact]
    public static void CheckSnapshot_Detects_Valid_Snapshot()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Assert.True(
            snapshotStore.CheckSnapshotExists(
                snapshotId,
                Fuzzy.Default));

        Assert.True(
            snapshotStore.CheckSnapshotValid(
                snapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void CheckSnapshot_Throws_If_Snapshot_Missing()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Remove(repository.Chunks, repository.Chunks.ListKeys());

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.CheckSnapshotExists(
                snapshotId,
                Fuzzy.Default));

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.CheckSnapshotValid(
                snapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void CheckSnapshot_Throws_If_Snapshot_Invalid()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Invalidate(repository.Chunks, repository.Chunks.ListKeys());

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.CheckSnapshotExists(
                snapshotId,
                Fuzzy.Default));

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.CheckSnapshotValid(
                snapshotId,
                Fuzzy.Default));
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

        Remove(repository.Chunks, chunkIds);

        Assert.False(
            snapshotStore.CheckSnapshotExists(
                snapshotId,
                Fuzzy.Default));

        Assert.False(
            snapshotStore.CheckSnapshotValid(
                snapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void CheckSnapshot_Detects_Invalid_Blob()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        var chunkIds = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences
            .SelectMany(b => b.ChunkIds);

        Invalidate(repository.Chunks, chunkIds);

        Assert.True(
           snapshotStore.CheckSnapshotExists(
               snapshotId,
               Fuzzy.Default));

        Assert.False(
            snapshotStore.CheckSnapshotValid(
                snapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void CheckSnapshot_Throws_When_Empty()
    {
        var snapshotStore = Some.SnapshotStore();

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.CheckSnapshotExists(
                SnapshotStore.LatestSnapshotId,
                Fuzzy.Default));

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.CheckSnapshotValid(
                SnapshotStore.LatestSnapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void RestoreSnapshot_Writes_Ordered_Blob_Chunks_To_Stream()
    {
        var fastCdc = new FastCdc(256, 1024, 2048);
        var snapshotStore = Some.SnapshotStore(fastCdc: fastCdc);
        var blobs = Some.Blobs();

        // Create data that is large enough to create at least two chunks
        var expectedBytes = Some.RandomNumber(2 * fastCdc.MaxSize);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(blobs, _ => expectedBytes));

        var blobSystem = Some.BlobSystem();

        snapshotStore.RestoreSnapshot(
            blobSystem,
            snapshotId,
            Fuzzy.Default);

        var blobReferences = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences
            .ToArray();

        Assert.Equal(blobs, blobSystem.ListBlobs());
        Assert.NotEmpty(blobReferences);

        for (var i = 0; i < blobReferences.Length; i++)
        {
            var blobReference = blobReferences[i];

            using var blobStream = blobSystem.OpenRead(blobs[i].Name);
            using var memoryStream = new MemoryStream();

            snapshotStore.RestoreChunks(blobReference.ChunkIds, memoryStream);

            Assert.True(blobReference.ChunkIds.Count > 1);
            Assert.Equal(expectedBytes, ToBytes(blobStream));
            Assert.Equal(expectedBytes, memoryStream.ToArray());
        }
    }

    [Fact]
    public static void RestoreSnapshot_Does_Not_Overwrite_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);

        Assert.Throws<AggregateException>(
            () => snapshotStore.RestoreSnapshot(
                blobSystem,
                snapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void RestoreSnapshot_Can_Write_Empty_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobs = Some.Blobs();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(
                blobs,
                _ => Array.Empty<byte>()));

        var blobSystem = Some.BlobSystem();

        snapshotStore.RestoreSnapshot(
            blobSystem,
            snapshotId,
            Fuzzy.Default);

        var restoredBlobs = blobSystem.ListBlobs();

        Assert.Equal(blobs, restoredBlobs);

        foreach (var blob in restoredBlobs)
        {
            using var stream = blobSystem.OpenRead(blob.Name);

            Assert.Empty(ToBytes(stream));
        }
    }

    [Fact]
    public static void RestoreSnapshot_Throws_When_Empty()
    {
        var snapshotStore = Some.SnapshotStore();

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.RestoreSnapshot(
                Some.BlobSystem(),
                SnapshotStore.LatestSnapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void RestoreSnapshot_Throws_Given_Wrong_Key()
    {
        var snapshotId = Some.SnapshotStore(password: "a").StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Assert.Throws<ChunkyardException>(
            () => Some.SnapshotStore(password: "b").RestoreSnapshot(
                Some.BlobSystem(),
                snapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void FilterSnapshot_Lists_Matching_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();
        var expectedBlob = Some.Blob("blob1");

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(new[] { expectedBlob, Some.Blob("blob2") }));

        var blobReferences = snapshotStore.FilterSnapshot(
            snapshotId,
            new Fuzzy(new[] { expectedBlob.Name }));

        Assert.Equal(
            new[] { expectedBlob },
            blobReferences.Select(br => br.Blob));
    }

    [Fact]
    public static void FilterSnapshot_Throws_When_Empty()
    {
        var snapshotStore = Some.SnapshotStore();

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.FilterSnapshot(
                SnapshotStore.LatestSnapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void GarbageCollect_Keeps_Used_Ids()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        snapshotStore.GarbageCollect();

        Assert.True(
            snapshotStore.CheckSnapshotValid(
                snapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void GarbageCollect_Removes_Unused_Ids()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        snapshotStore.RemoveSnapshot(snapshotId);

        Assert.NotEmpty(repository.Chunks.ListKeys());

        snapshotStore.GarbageCollect();

        Assert.Empty(repository.Chunks.ListKeys());
        Assert.Empty(repository.Snapshots.ListKeys());
    }

    [Fact]
    public static void RemoveSnapshot_Removes_Existing_Snapshots()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);

        snapshotStore.StoreSnapshot(blobSystem);
        snapshotStore.RemoveSnapshot(snapshotId);
        snapshotStore.RemoveSnapshot(SnapshotStore.LatestSnapshotId);

        Assert.Empty(snapshotStore.ListSnapshotIds());
    }

    [Fact]
    public static void RemoveSnapshot_Throws_When_Empty()
    {
        var snapshotStore = Some.SnapshotStore();

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.RemoveSnapshot(
                SnapshotStore.LatestSnapshotId));
    }

    [Fact]
    public static void KeepSnapshots_Removes_Previous_Snapshots()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        snapshotStore.StoreSnapshot(blobSystem);

        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);

        snapshotStore.KeepSnapshots(1);

        Assert.Equal(
            new[] { snapshotId },
            snapshotStore.ListSnapshotIds());
    }

    [Fact]
    public static void KeepSnapshots_Does_Nothing_If_Equals_Or_Greater_Than_Current()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        snapshotStore.KeepSnapshots(1);
        snapshotStore.KeepSnapshots(2);

        Assert.Equal(
            new[] { snapshotId },
            snapshotStore.ListSnapshotIds());
    }

    [Fact]
    public static void KeepSnapshots_Zero_Input_Empties_Store()
    {
        var snapshotStore = Some.SnapshotStore();

        snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        snapshotStore.KeepSnapshots(0);

        Assert.Empty(snapshotStore.ListSnapshotIds());
    }

    [Fact]
    public static void KeepSnapshots_Negative_Input_Empties_Store()
    {
        var snapshotStore = Some.SnapshotStore();

        snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        snapshotStore.KeepSnapshots(-1);

        Assert.Empty(snapshotStore.ListSnapshotIds());
    }

    [Fact]
    public static void CopyTo_Copies_Newer_Snapshots()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);
        var otherRepository = Some.Repository();

        snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs("some blob")));

        snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs("some blob", "other blob")));

        snapshotStore.CopyTo(otherRepository);

        Assert.Equal(
            repository.Chunks.ListKeys(),
            otherRepository.Chunks.ListKeys());

        Assert.Equal(
            repository.Snapshots.ListKeys(),
            otherRepository.Snapshots.ListKeys());
    }

    [Fact]
    public static void CopyTo_Throws_On_Invalid_Chunk()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Invalidate(repository.Chunks, repository.Chunks.ListKeys());

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.CopyTo(
                Some.Repository()));
    }

    [Fact]
    public static void CopyTo_Throws_On_Invalid_References()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Invalidate(repository.Snapshots, repository.Snapshots.ListKeys());

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.CopyTo(
                Some.Repository()));
    }

    [Fact]
    public static void Mirror_Updates_Known_Blobs_And_Removes_Unknown_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();
        var expectedBlobs = Some.Blobs();
        var expectedBytes = new byte[] { 0xAB, 0xCD, 0xEF };
        var blobSystem = Some.BlobSystem(expectedBlobs, _ => expectedBytes);

        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);
        var blobToDelete = Some.Blob("blob to delete");

        using (var writeStream = blobSystem.NewWrite(blobToDelete))
        {
            writeStream.Write(new byte[] { 0x10, 0x11 });
        }

        var blobToUpdate = new Blob(
            expectedBlobs.Last().Name,
            expectedBlobs.Last().LastWriteTimeUtc.AddHours(1));

        using (var writeStream = blobSystem.OpenWrite(blobToUpdate))
        {
            writeStream.Write(new byte[] { 0x10, 0x11 });
        }

        snapshotStore.MirrorSnapshot(blobSystem, snapshotId);

        Assert.Equal(expectedBlobs, blobSystem.ListBlobs());

        foreach (var blob in expectedBlobs)
        {
            using var blobStream = blobSystem.OpenRead(blob.Name);

            Assert.Equal(expectedBytes, ToBytes(blobStream));
        }
    }

    [Fact]
    public static void StoreSnapshotPreview_Shows_Preview()
    {
        var snapshotStore = Some.SnapshotStore();
        var initialBlobSystem = Some.BlobSystem(Some.Blobs("some blob"));

        var initialDiff = snapshotStore.StoreSnapshotPreview(
            initialBlobSystem);

        snapshotStore.StoreSnapshot(initialBlobSystem);

        var changedBlobSystem = Some.BlobSystem(
            Some.Blobs("other blob"));

        var changedDiff = snapshotStore.StoreSnapshotPreview(
            changedBlobSystem);

        Assert.Equal(
            new DiffSet(
                new[] { "some blob" },
                Array.Empty<string>(),
                Array.Empty<string>()),
            initialDiff);

        Assert.Equal(
            new DiffSet(
                new[] { "other blob" },
                Array.Empty<string>(),
                new[] { "some blob" }),
            changedDiff);
    }

    [Fact]
    public static void MirrorSnapshotPreview_Shows_Preview()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs("some blob")));

        var actualDiff = snapshotStore.MirrorSnapshotPreview(
            Some.BlobSystem(Some.Blobs("other blob")),
            snapshotId);

        Assert.Equal(
            new DiffSet(
                new[] { "some blob" },
                Array.Empty<string>(),
                new[] { "other blob" }),
            actualDiff);
    }

    private static void Remove<T>(
        IRepository<T> repository,
        IEnumerable<T> keys)
    {
        foreach (var key in keys)
        {
            repository.RemoveValue(key);
        }
    }

    private static void Invalidate<T>(
        IRepository<T> repository,
        IEnumerable<T> keys)
    {
        foreach (var key in keys)
        {
            var value = repository.RetrieveValue(key);

            repository.RemoveValue(key);
            repository.StoreValue(
                key,
                value.Concat(new byte[] { 0xFF }).ToArray());
        }
    }

    private static byte[] ToBytes(Stream stream)
    {
        using var memoryStream = new MemoryStream();

        stream.CopyTo(memoryStream);

        return memoryStream.ToArray();
    }
}
