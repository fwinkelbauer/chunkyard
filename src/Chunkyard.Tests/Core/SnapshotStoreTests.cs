namespace Chunkyard.Tests.Core;

public static class SnapshotStoreTests
{
    [Fact]
    public static void StoreSnapshot_Stores_A_Snapshot_Of_Blobs_With_Distinct_Nonces()
    {
        var snapshotStore = Some.SnapshotStore();
        var expectedBlobs = Some.Blobs();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(expectedBlobs));

        var snapshot = snapshotStore.GetSnapshot(snapshotId);

        var nonces = snapshot.BlobReferences.Select(br => br.Nonce)
            .ToArray();

        Assert.Equal(nonces, nonces.Distinct());

        Assert.Equal(
            new[] { snapshotId },
            snapshotStore.ListSnapshotIds());

        Assert.Equal(
            expectedBlobs,
            snapshot.BlobReferences.Select(br => br.Blob));
    }

    [Fact]
    public static void StoreSnapshot_Deduplicates_Chunks_And_Reuses_Nonces_For_Known_Blobs()
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

        var blobSystem1 = Some.BlobSystem(blobs, _ => new byte[] { 0x01 });
        var snapshotId1 = snapshotStore.StoreSnapshot(blobSystem1);

        var blobSystem2 = Some.BlobSystem(blobs, _ => new byte[] { 0x02 });
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
            Some.BlobSystem(Some.Blobs(), _ => Array.Empty<byte>()));

        var chunkIds = snapshotStore.GetSnapshot(snapshotId).BlobReferences
            .SelectMany(br => br.ChunkIds);

        Assert.Empty(chunkIds);
    }

    [Fact]
    public static void New_SnapshotStore_Instance_Can_Read_Existing_Snapshot()
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
        _ = snapshotStore.StoreSnapshot(blobSystem);

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

        Change(repository.Chunks, repository.Chunks.ListKeys());

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

        Change(repository.Chunks, chunkIds);

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
    public static void RestoreSnapshot_Writes_Ordered_Chunks_To_Blob_Stream()
    {
        var fastCdc = new FastCdc(256, 1024, 2048);
        var snapshotStore = Some.SnapshotStore(fastCdc: fastCdc);

        // Create data that is large enough to create at least two chunks
        var expectedBytes = Some.RandomNumber(2 * fastCdc.MaxSize);
        var inputBlobSystem = Some.BlobSystem(Some.Blobs(), _ => expectedBytes);
        var expectedContent = Content(inputBlobSystem);

        var snapshotId = snapshotStore.StoreSnapshot(inputBlobSystem);

        var outputBlobSystem = Some.BlobSystem();

        snapshotStore.RestoreSnapshot(
            outputBlobSystem,
            snapshotId,
            Fuzzy.Default);

        Assert.Equal(
            expectedContent,
            Content(outputBlobSystem));

        var blobReferences = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences
            .ToArray();

        var chunkIds = blobReferences.SelectMany(br => br.ChunkIds).ToArray();

        Assert.True(blobReferences.Length > 0
            && blobReferences.Length * 2 <= chunkIds.Length);
    }

    [Fact]
    public static void RestoreSnapshot_Does_Not_Overwrite_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);

        var expectedContent = Content(blobSystem);

        Assert.Throws<AggregateException>(
            () => snapshotStore.RestoreSnapshot(
                blobSystem,
                snapshotId,
                Fuzzy.Default));

        Assert.Equal(
            expectedContent,
            Content(blobSystem));
    }

    [Fact]
    public static void RestoreSnapshot_Can_Write_Empty_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();
        var inputBlobSystem = Some.BlobSystem(
            Some.Blobs(),
            _ => Array.Empty<byte>());

        var expectedContent = Content(inputBlobSystem);

        var snapshotId = snapshotStore.StoreSnapshot(inputBlobSystem);

        var outputBlobSystem = Some.BlobSystem();

        snapshotStore.RestoreSnapshot(
            outputBlobSystem,
            snapshotId,
            Fuzzy.Default);

        Assert.Equal(
            expectedContent,
            Content(outputBlobSystem));
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
        _ = snapshotStore.StoreSnapshot(blobSystem);

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
    public static void KeepSnapshots_Removes_Older_Snapshots()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        _ = snapshotStore.StoreSnapshot(blobSystem);
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

        _ = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs("some blob")));

        _ = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs("some blob", "other blob")));

        snapshotStore.CopyTo(otherRepository);

        _ = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs("different blob")));

        snapshotStore.CopyTo(otherRepository);

        Assert.Equal(
            repository.Chunks.ListKeys(),
            otherRepository.Chunks.ListKeys());

        Assert.Equal(
            repository.Snapshots.ListKeys(),
            otherRepository.Snapshots.ListKeys());
    }

    [Fact]
    public static void CopyTo_Throws_On_Invalid_Chunks()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        var chunkIds = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences
            .SelectMany(b => b.ChunkIds);

        Change(repository.Chunks, chunkIds);

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.CopyTo(
                Some.Repository()));
    }

    [Fact]
    public static void CopyTo_Throws_On_Invalid_References()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        _ = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Change(repository.Snapshots, repository.Snapshots.ListKeys());

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.CopyTo(
                Some.Repository()));
    }

    [Fact]
    public static void Mirror_Updates_Known_Blobs_And_Removes_Unknown_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobs = Some.Blobs();
        var blobSystem = Some.BlobSystem(
            blobs,
            _ => new byte[] { 0xAB, 0xCD, 0xEF });

        var expectedContent = Content(blobSystem);

        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);
        var blobToDelete = Some.Blob("blob to delete");

        Change(blobSystem, blobToDelete);

        var blobToUpdate = new Blob(
            blobs.Last().Name,
            blobs.Last().LastWriteTimeUtc.AddHours(1));

        Change(blobSystem, blobToUpdate);

        snapshotStore.MirrorSnapshot(blobSystem, snapshotId);

        Assert.Equal(
            expectedContent,
            Content(blobSystem));
    }

    [Fact]
    public static void StoreSnapshotPreview_Shows_Preview()
    {
        var snapshotStore = Some.SnapshotStore();
        var initialBlobSystem = Some.BlobSystem(Some.Blobs("some blob"));

        var initialDiff = snapshotStore.StoreSnapshotPreview(
            initialBlobSystem);

        _ = snapshotStore.StoreSnapshot(initialBlobSystem);

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

    private static void Change<T>(
        IRepository<T> repository,
        IEnumerable<T> keys)
    {
        foreach (var key in keys)
        {
            var bytes = repository.RetrieveValue(key);

            repository.RemoveValue(key);
            repository.StoreValue(
                key,
                bytes.Concat(new byte[] { 0xFF }).ToArray());
        }
    }

    private static void Change(
        IBlobSystem blobSystem,
        Blob blob)
    {
        using var memoryStream = new MemoryStream();

        if (blobSystem.BlobExists(blob.Name))
        {
            using var readStream = blobSystem.OpenRead(blob.Name);
            memoryStream.CopyTo(readStream);
        }

        memoryStream.Write(new byte[] { 0xAB, 0xCD });

        using var writeStream = blobSystem.OpenWrite(blob);

        memoryStream.CopyTo(writeStream);
    }

    private static IReadOnlyDictionary<Blob, byte[]> Content(
        IBlobSystem blobSystem)
    {
        return blobSystem.ListBlobs().ToDictionary(
            blob => blob,
            blob =>
            {
                using var memoryStream = new MemoryStream();
                using var blobStream = blobSystem.OpenRead(blob.Name);

                blobStream.CopyTo(memoryStream);

                return memoryStream.ToArray();
            });
    }
}
