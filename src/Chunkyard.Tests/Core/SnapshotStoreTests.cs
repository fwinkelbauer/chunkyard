namespace Chunkyard.Tests.Core;

public static class SnapshotStoreTests
{
    [Fact]
    public static void StoreSnapshot_Stores_A_Snapshot_Of_Blobs()
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

        Assert.Equal(snapshot1.BlobReferences, snapshot2.BlobReferences);
    }

    [Fact]
    public static void StoreSnapshot_Does_Not_Deduplicate_Chunks_For_Blobs_With_Changed_LastWriteTimeUtc()
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

        Assert.Equal(
            blobReferences1.Select(br => br.Blob.Name),
            blobReferences2.Select(br => br.Blob.Name));

        Assert.NotEqual(
            blobReferences1.SelectMany(br => br.ChunkIds),
            blobReferences2.SelectMany(br => br.ChunkIds));
    }

    [Fact]
    public static void StoreSnapshot_Can_Store_Empty_Snapshot()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(Some.BlobSystem());
        var snapshot = snapshotStore.GetSnapshot(snapshotId);

        Assert.Empty(snapshot.BlobReferences);
    }

    [Fact]
    public static void StoreSnapshot_Can_Store_Empty_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs(), _ => Array.Empty<byte>()));

        var snapshot = snapshotStore.GetSnapshot(snapshotId);
        var chunkIds = snapshot.BlobReferences.SelectMany(br => br.ChunkIds);

        Assert.NotEmpty(snapshot.BlobReferences);
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

        Assert.Equal(expectedBlobs, actualBlobs);
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

        Assert.NotNull(
            snapshotStore.GetSnapshotReference(snapshotId));
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
            snapshotStore.GetSnapshot(SnapshotStore.SecondLatestSnapshotId));
    }

    [Fact]
    public static void GetSnapshot_Throws_If_Snapshot_Does_Not_Exist()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Assert.ThrowsAny<Exception>(
            () => snapshotStore.GetSnapshot(snapshotId + 1));
    }

    [Fact]
    public static void GetSnapshot_Throws_If_Empty()
    {
        var snapshotStore = Some.SnapshotStore();

        Assert.ThrowsAny<Exception>(
            () => snapshotStore.GetSnapshot(0));
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
    public static void CheckSnapshot_Throws_If_Missing_Snapshot()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Missing(repository.Chunks, repository.Chunks.List());

        Assert.ThrowsAny<Exception>(
            () => snapshotStore.CheckSnapshotExists(
                snapshotId,
                Fuzzy.Default));

        Assert.ThrowsAny<Exception>(
            () => snapshotStore.CheckSnapshotValid(
                snapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void CheckSnapshot_Throws_If_Corrupt_Snapshot()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Corrupt(repository.Chunks, repository.Chunks.List());

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

        Missing(repository.Chunks, chunkIds);

        Assert.False(
            snapshotStore.CheckSnapshotExists(
                snapshotId,
                Fuzzy.Default));

        Assert.False(
            snapshotStore.CheckSnapshotValid(
                snapshotId,
                Fuzzy.Default));

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.EnsureSnapshotExists(
                snapshotId,
                Fuzzy.Default));

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.EnsureSnapshotValid(
                snapshotId,
                Fuzzy.Default));
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
           snapshotStore.CheckSnapshotExists(
               snapshotId,
               Fuzzy.Default));

        Assert.False(
            snapshotStore.CheckSnapshotValid(
                snapshotId,
                Fuzzy.Default));

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.EnsureSnapshotValid(
                snapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void RestoreSnapshot_Writes_Ordered_Chunks_To_Blob_Stream()
    {
        var fastCdc = new FastCdc(256, 1024, 2048);
        var snapshotStore = Some.SnapshotStore(fastCdc: fastCdc);

        // Create data that is large enough to create at least two chunks
        var expectedBytes = RandomNumberGenerator.GetBytes(2 * fastCdc.MaxSize);
        var inputBlobSystem = Some.BlobSystem(Some.Blobs(), _ => expectedBytes);

        var snapshotId = snapshotStore.StoreSnapshot(inputBlobSystem);

        var outputBlobSystem = Some.BlobSystem();

        snapshotStore.RestoreSnapshot(
            outputBlobSystem,
            snapshotId,
            Fuzzy.Default);

        var blobReferences = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences
            .ToArray();

        var chunkIds = blobReferences.SelectMany(br => br.ChunkIds).ToArray();

        Assert.Equal(
            ToContent(inputBlobSystem),
            ToContent(outputBlobSystem));

        Assert.True(blobReferences.Length > 0
            && blobReferences.Length * 2 <= chunkIds.Length);
    }

    [Fact]
    public static void RestoreSnapshot_Updates_Known_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobs = Some.Blobs(
            "blob to restore",
            "unchanged blob",
            "changed blob");

        static byte[] Generator(string _) => new byte[] { 0xAB, 0xCD, 0xEF };

        var inputBlobSystem = Some.BlobSystem(blobs, Generator);
        var outputBlobSystem = Some.BlobSystem(
            new[]
            {
                blobs[1],
                new Blob(
                    blobs[2].Name,
                    blobs[2].LastWriteTimeUtc.AddHours(1))
            },
            Generator);

        var snapshotId = snapshotStore.StoreSnapshot(inputBlobSystem);

        snapshotStore.RestoreSnapshot(
            outputBlobSystem,
            snapshotId,
            Fuzzy.Default);

        Assert.Equal(
            ToContent(inputBlobSystem),
            ToContent(outputBlobSystem));
    }

    [Fact]
    public static void RestoreSnapshot_Can_Write_Empty_Blobs()
    {
        var snapshotStore = Some.SnapshotStore();
        var inputBlobSystem = Some.BlobSystem(
            Some.Blobs(),
            _ => Array.Empty<byte>());

        var expectedContent = ToContent(inputBlobSystem);

        var snapshotId = snapshotStore.StoreSnapshot(inputBlobSystem);

        var outputBlobSystem = Some.BlobSystem();

        snapshotStore.RestoreSnapshot(
            outputBlobSystem,
            snapshotId,
            Fuzzy.Default);

        Assert.Equal(
            expectedContent,
            ToContent(outputBlobSystem));
    }

    [Fact]
    public static void RestoreSnapshot_Throws_Given_Wrong_Key()
    {
        var snapshotId = Some.SnapshotStore(password: "a").StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Assert.ThrowsAny<Exception>(
            () => Some.SnapshotStore(password: "b").RestoreSnapshot(
                Some.BlobSystem(),
                snapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void RestoreSnapshotPreview_Shows_Preview()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs("some blob")));

        var actualDiff = snapshotStore.RestoreSnapshotPreview(
            Some.BlobSystem(Some.Blobs("new blob")),
            snapshotId,
            Fuzzy.Default);

        Assert.Equal(
            new DiffSet(
                new[] { "some blob" },
                Array.Empty<string>(),
                Array.Empty<string>()),
            actualDiff);
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
            new Fuzzy(expectedBlob.Name));

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

        Assert.NotEmpty(repository.Chunks.List());

        snapshotStore.GarbageCollect();

        Assert.Empty(repository.Chunks.List());
        Assert.Empty(repository.Snapshots.List());
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
    public static void RemoveSnapshot_Throws_If_Empty()
    {
        var snapshotStore = Some.SnapshotStore();

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.RemoveSnapshot(SnapshotStore.LatestSnapshotId));
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

        for (var i = 0; i < 5; i++)
        {
            _ = snapshotStore.StoreSnapshot(blobSystem);
        }

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
            repository.Chunks.List().OrderBy(k => k),
            otherRepository.Chunks.List().OrderBy(k => k));

        Assert.Equal(
            repository.Snapshots.List().OrderBy(k => k),
            otherRepository.Snapshots.List().OrderBy(k => k));
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

        Assert.Single(otherSnapshotStore.ListSnapshotIds());

        Assert.True(
            otherSnapshotStore.CheckSnapshotValid(snapshotId, Fuzzy.Default));
    }

    [Fact]
    public static void CopyTo_Throws_On_Corrupt_Chunks()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        var chunkIds = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences
            .SelectMany(b => b.ChunkIds);

        Corrupt(repository.Chunks, chunkIds);

        Assert.Throws<AggregateException>(
            () => snapshotStore.CopyTo(
                Some.Repository()));
    }

    [Fact]
    public static void CopyTo_Throws_On_Corrupt_References()
    {
        var repository = Some.Repository();
        var snapshotStore = Some.SnapshotStore(repository);

        _ = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Corrupt(repository.Snapshots, repository.Snapshots.List());

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.CopyTo(
                Some.Repository()));
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

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.CopyTo(otherRepository));
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

    private static IReadOnlyDictionary<Blob, byte[]> ToContent(
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
