namespace Chunkyard.Tests.Core;

public static class SnapshotStoreTests
{
    [Fact]
    public static void StoreSnapshot_Creates_Snapshot()
    {
        var snapshotStore = CreateSnapshotStore();
        var expectedBlobs = CreateBlobs("some blob");

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(expectedBlobs),
            Fuzzy.Default,
            DateTime.UtcNow);

        var snapshot = snapshotStore.GetSnapshot(snapshotId);

        Assert.Equal(
            new[] { snapshotId },
            snapshotStore.ListSnapshotIds());

        Assert.Equal(
            expectedBlobs,
            snapshot.BlobReferences.Select(br => br.Blob));
    }

    [Fact]
    public static void StoreSnapshot_Fuzzy_Excludes_Blobs()
    {
        var snapshotStore = CreateSnapshotStore();
        var blobs = CreateBlobs("some blob", "other blob");

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(blobs),
            new Fuzzy(new[] { "other" }),
            DateTime.UtcNow);

        var snapshot = snapshotStore.GetSnapshot(snapshotId);

        Assert.Equal(
            new[] { blobs[0] },
            snapshot.BlobReferences.Select(br => br.Blob));
    }

    [Fact]
    public static void StoreSnapshot_Deduplicates_Blobs_Using_Same_Nonce()
    {
        var snapshotStore = CreateSnapshotStore();

        var snapshotId1 = snapshotStore.StoreSnapshot(
            CreateBlobSystem(
                CreateBlobs("some blob")),
            Fuzzy.Default,
            DateTime.UtcNow);

        var snapshotId2 = snapshotStore.StoreSnapshot(
            CreateBlobSystem(
                CreateBlobs("some blob")),
            Fuzzy.Default,
            DateTime.UtcNow);

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
        var snapshotStore = CreateSnapshotStore();
        var blobs = CreateBlobs();

        var blobSystem1 = CreateBlobSystem(
            blobs,
            blobName => new byte[] { 0x11, 0x11 });

        var snapshotId1 = snapshotStore.StoreSnapshot(
            blobSystem1,
            Fuzzy.Default,
            DateTime.UtcNow);

        var blobSystem2 = CreateBlobSystem(
            blobs,
            blobName => new byte[] { 0x22, 0x22 });

        var snapshotId2 = snapshotStore.StoreSnapshot(
            blobSystem2,
            Fuzzy.Default,
            DateTime.UtcNow);

        var snapshot1 = snapshotStore.GetSnapshot(snapshotId1);
        var snapshot2 = snapshotStore.GetSnapshot(snapshotId2);

        Assert.Equal(
            snapshot1.BlobReferences,
            snapshot2.BlobReferences);
    }

    [Fact]
    public static void StoreSnapshot_Can_Create_Snapshot_Without_Any_Data()
    {
        var snapshotStore = CreateSnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(),
            Fuzzy.Default,
            DateTime.UtcNow);

        var snapshot = snapshotStore.GetSnapshot(snapshotId);

        Assert.Empty(snapshot.BlobReferences);
    }

    [Fact]
    public static void StoreSnapshot_Stores_Empty_Blob_Without_Chunks()
    {
        var snapshotStore = CreateSnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(
                CreateBlobs("empty file"),
                blobName => Array.Empty<byte>()),
            Fuzzy.Default,
            DateTime.UtcNow);

        var chunkIds = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences
            .First()
            .ChunkIds;

        Assert.Empty(chunkIds);
    }

    [Fact]
    public static void New_SnapshotStore_Can_Read_Existing_Snapshot()
    {
        var repository = CreateRepository();
        var expectedBlobs = CreateBlobs("some blob");

        var snapshotId = CreateSnapshotStore(repository)
            .StoreSnapshot(
                CreateBlobSystem(expectedBlobs),
                Fuzzy.Default,
                DateTime.UtcNow);

        var actualBlobs = CreateSnapshotStore(repository)
            .GetSnapshot(snapshotId).BlobReferences
            .Select(br => br.Blob);

        Assert.Equal(
            expectedBlobs,
            actualBlobs);
    }

    [Fact]
    public static void GetSnapshot_Accepts_Negative_SnapshotIds()
    {
        var snapshotStore = CreateSnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        Assert.Equal(
            snapshotStore.GetSnapshot(snapshotId),
            snapshotStore.GetSnapshot(SnapshotStore.LatestSnapshotId));
    }

    [Fact]
    public static void GetSnapshot_Throws_If_Version_Does_Not_Exist()
    {
        var snapshotStore = CreateSnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.GetSnapshot(snapshotId + 1));

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.GetSnapshot(
                SnapshotStore.SecondLatestSnapshotId));
    }

    [Fact]
    public static void GetSnapshot_Accepts_Negative_SnapshotIds_With_Gaps()
    {
        var snapshotStore = CreateSnapshotStore();
        var blobSystem = CreateBlobSystem(CreateBlobs());

        var snapshotId = snapshotStore.StoreSnapshot(
            blobSystem,
            Fuzzy.Default,
            DateTime.UtcNow);

        var snapshotIdToRemove = snapshotStore.StoreSnapshot(
            blobSystem,
            Fuzzy.Default,
            DateTime.UtcNow);

        snapshotStore.StoreSnapshot(
            blobSystem,
            Fuzzy.Default,
            DateTime.UtcNow);

        snapshotStore.RemoveSnapshot(snapshotIdToRemove);

        Assert.Equal(
            snapshotStore.GetSnapshot(snapshotId),
            snapshotStore.GetSnapshot(
                SnapshotStore.SecondLatestSnapshotId));
    }

    [Fact]
    public static void GetSnapshot_Throws_On_Empty_SnapshotStore()
    {
        var snapshotStore = CreateSnapshotStore();

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.GetSnapshot(
                SnapshotStore.LatestSnapshotId));
    }

    [Fact]
    public static void CheckSnapshot_Detects_Valid_Snapshot()
    {
        var snapshotStore = CreateSnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

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
        var repository = CreateRepository();
        var snapshotStore = CreateSnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

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
        var repository = CreateRepository();
        var snapshotStore = CreateSnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

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
        var repository = CreateRepository();
        var snapshotStore = CreateSnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

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
        var repository = CreateRepository();
        var snapshotStore = CreateSnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

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
    public static void CheckSnapshot_Throws_On_Empty_SnapshotStore()
    {
        var snapshotStore = CreateSnapshotStore();

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
        var fastCdc = new FastCdc(64, 256, 1024);
        var snapshotStore = CreateSnapshotStore(
            fastCdc: fastCdc);

        var blobs = CreateBlobs("some blob");

        // Create data that is large enough to create at least two chunks
        var expectedBytes = GenerateRandomNumber(fastCdc.MaxSize + 1);

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(
                blobs,
                _ => expectedBytes),
            Fuzzy.Default,
            DateTime.UtcNow);

        var blobSystem = CreateBlobSystem();

        var restoredBlobs = snapshotStore.RestoreSnapshot(
            blobSystem,
            snapshotId,
            Fuzzy.Default);

        var blobReferences = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences;

        Assert.Equal(blobs, restoredBlobs);
        Assert.NotEmpty(blobReferences);

        foreach (var blobReference in blobReferences)
        {
            using var blobStream = blobSystem.OpenRead(blobs[0].Name);
            using var memoryStream = new MemoryStream();

            snapshotStore.RestoreChunks(
                blobReference.ChunkIds,
                memoryStream);

            Assert.True(blobReference.ChunkIds.Count > 1);
            Assert.Equal(expectedBytes, ToBytes(blobStream));
            Assert.Equal(expectedBytes, memoryStream.ToArray());
        }
    }

    [Fact]
    public static void RestoreSnapshot_Does_Not_Overwrite_Blob()
    {
        var snapshotStore = CreateSnapshotStore();

        var blobSystem = CreateBlobSystem(
            CreateBlobs("some blob"));

        var snapshotId = snapshotStore.StoreSnapshot(
            blobSystem,
            Fuzzy.Default,
            DateTime.UtcNow);

        Assert.Throws<AggregateException>(
            () => snapshotStore.RestoreSnapshot(
                blobSystem,
                snapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void RestoreSnapshot_Can_Write_Empty_Blobs()
    {
        var snapshotStore = CreateSnapshotStore();
        var blobs = CreateBlobs("some empty blob");

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(
                blobs,
                _ => Array.Empty<byte>()),
            Fuzzy.Default,
            DateTime.UtcNow);

        var blobSystem = CreateBlobSystem();

        var restoredBlobs = snapshotStore.RestoreSnapshot(
            blobSystem,
            snapshotId,
            Fuzzy.Default);

        Assert.Equal(blobs, restoredBlobs);

        foreach (var blob in restoredBlobs)
        {
            using var stream = blobSystem.OpenRead(blob.Name);

            Assert.Empty(ToBytes(stream));
        }
    }

    [Fact]
    public static void RestoreSnapshot_Throws_On_Empty_SnapshotStore()
    {
        var snapshotStore = CreateSnapshotStore();

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.RestoreSnapshot(
                CreateBlobSystem(),
                SnapshotStore.LatestSnapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void RestoreSnapshot_Throws_Given_Wrong_Key()
    {
        var snapshotId = CreateSnapshotStore(password: "a").StoreSnapshot(
            CreateBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        Assert.Throws<ChunkyardException>(
            () => CreateSnapshotStore(password: "b").RestoreSnapshot(
                CreateBlobSystem(),
                snapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void FilterSnapshot_Lists_Blobs()
    {
        var snapshotStore = CreateSnapshotStore();
        var expectedBlobs = CreateBlobs();

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(expectedBlobs),
            Fuzzy.Default,
            DateTime.UtcNow);

        var blobReferences = snapshotStore.FilterSnapshot(
            snapshotId,
            Fuzzy.Default);

        Assert.Equal(
            expectedBlobs,
            blobReferences.Select(br => br.Blob));
    }

    [Fact]
    public static void ShowSnapshot_Throws_On_Empty_SnapshotStore()
    {
        var snapshotStore = CreateSnapshotStore();

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.FilterSnapshot(
                SnapshotStore.LatestSnapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void GarbageCollect_Keeps_Used_Ids()
    {
        var snapshotStore = CreateSnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        Assert.Empty(snapshotStore.GarbageCollect());

        Assert.True(
            snapshotStore.CheckSnapshotValid(
                snapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void GarbageCollect_Removes_Unused_Ids()
    {
        var repository = CreateRepository();
        var snapshotStore = CreateSnapshotStore(repository);

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        snapshotStore.RemoveSnapshot(snapshotId);
        snapshotStore.GarbageCollect();

        Assert.Empty(repository.Chunks.ListKeys());
    }

    [Fact]
    public static void RemoveSnapshot_Removes_Existing_Snapshots()
    {
        var snapshotStore = CreateSnapshotStore();
        var blobSystem = CreateBlobSystem(CreateBlobs());

        var snapshotId = snapshotStore.StoreSnapshot(
            blobSystem,
            Fuzzy.Default,
            DateTime.UtcNow);

        snapshotStore.StoreSnapshot(
            blobSystem,
            Fuzzy.Default,
            DateTime.UtcNow);

        snapshotStore.RemoveSnapshot(snapshotId);
        snapshotStore.RemoveSnapshot(SnapshotStore.LatestSnapshotId);

        Assert.Empty(snapshotStore.ListSnapshotIds());
    }

    [Fact]
    public static void RemoveSnapshot_Throws_On_Empty_SnapshotStore()
    {
        var snapshotStore = CreateSnapshotStore();

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.RemoveSnapshot(
                SnapshotStore.LatestSnapshotId));
    }

    [Fact]
    public static void KeepSnapshots_Removes_Previous_Snapshots()
    {
        var snapshotStore = CreateSnapshotStore();
        var blobSystem = CreateBlobSystem(CreateBlobs());

        snapshotStore.StoreSnapshot(
            blobSystem,
            Fuzzy.Default,
            DateTime.UtcNow);

        var snapshotId = snapshotStore.StoreSnapshot(
            blobSystem,
            Fuzzy.Default,
            DateTime.UtcNow);

        snapshotStore.KeepSnapshots(1);

        Assert.Equal(
            new[] { snapshotId },
            snapshotStore.ListSnapshotIds());
    }

    [Fact]
    public static void KeepSnapshots_Does_Nothing_If_Equals_Or_Greater_Than_Current()
    {
        var snapshotStore = CreateSnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        snapshotStore.KeepSnapshots(1);
        snapshotStore.KeepSnapshots(2);

        Assert.Equal(
            new[] { snapshotId },
            snapshotStore.ListSnapshotIds());
    }

    [Fact]
    public static void KeepSnapshots_Zero_Input_Empties_Store()
    {
        var snapshotStore = CreateSnapshotStore();

        snapshotStore.StoreSnapshot(
            CreateBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        snapshotStore.KeepSnapshots(0);

        Assert.Empty(snapshotStore.ListSnapshotIds());
    }

    [Fact]
    public static void KeepSnapshots_Negative_Input_Empties_Store()
    {
        var snapshotStore = CreateSnapshotStore();

        snapshotStore.StoreSnapshot(
            CreateBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        snapshotStore.KeepSnapshots(-1);

        Assert.Empty(snapshotStore.ListSnapshotIds());
    }

    [Fact]
    public static void CopyTo_Copies_Newer_Snapshots()
    {
        var repository = CreateRepository();
        var snapshotStore = CreateSnapshotStore(repository);
        var otherRepository = CreateRepository();

        for (var i = 0; i < 3; i++)
        {
            snapshotStore.StoreSnapshot(
                CreateBlobSystem(CreateBlobs()),
                Fuzzy.Default,
                DateTime.UtcNow);

            snapshotStore.CopyTo(otherRepository);

            Assert.Equal(
                repository.Chunks.ListKeys().OrderBy(chunkId => chunkId),
                otherRepository.Chunks.ListKeys().OrderBy(chunkId => chunkId));

            Assert.Equal(
                repository.Snapshots.ListKeys(),
                otherRepository.Snapshots.ListKeys());
        }
    }

    [Fact]
    public static void CopyTo_Throws_On_Invalid_Chunk()
    {
        var repository = CreateRepository();
        var snapshotStore = CreateSnapshotStore(repository);

        snapshotStore.StoreSnapshot(
            CreateBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        Invalidate(repository.Chunks, repository.Chunks.ListKeys());

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.CopyTo(
                CreateRepository()));
    }

    [Fact]
    public static void CopyTo_Throws_On_Invalid_References()
    {
        var repository = CreateRepository();
        var snapshotStore = CreateSnapshotStore(repository);

        snapshotStore.StoreSnapshot(
            CreateBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        Invalidate(repository.Snapshots, repository.Snapshots.ListKeys());

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.CopyTo(
                CreateRepository()));
    }

    [Fact]
    public static void Mirror_Restores_Blobs_And_Removes_Blobs_Not_In_Snapshot()
    {
        var snapshotStore = CreateSnapshotStore();
        var expectedBlobs = CreateBlobs("some blob", "some other blob");

        var blobSystem = CreateBlobSystem(expectedBlobs);

        var snapshotId = snapshotStore.StoreSnapshot(
            blobSystem,
            Fuzzy.Default,
            DateTime.UtcNow);

        var blobToDelete = new Blob("blob to delete", DateTime.UtcNow);
        var otherBlob = new Blob("some other blob", DateTime.UtcNow);

        using (var writeStream = blobSystem.NewWrite(blobToDelete))
        {
            writeStream.Write(new byte[] { 0x10, 0x11 });
        }

        using (var writeStream = blobSystem.OpenWrite(otherBlob))
        {
            writeStream.Write(new byte[] { 0x10, 0x11 });
        }

        var restoredBlobs = snapshotStore.MirrorSnapshot(
            blobSystem,
            Fuzzy.Default,
            snapshotId);

        Assert.Equal(
            expectedBlobs,
            restoredBlobs);

        Assert.Equal(
            expectedBlobs.Select(b => b.Name),
            blobSystem.ListBlobs(Fuzzy.Default).Select(b => b.Name));
    }

    [Fact]
    public static void StoreSnapshotPreview_Shows_Preview()
    {
        var snapshotStore = CreateSnapshotStore();

        var initialBlobSystem = CreateBlobSystem(
            CreateBlobs("some blob"));

        var initialDiff = snapshotStore.StoreSnapshotPreview(
            initialBlobSystem,
            Fuzzy.Default);

        snapshotStore.StoreSnapshot(
            initialBlobSystem,
            Fuzzy.Default,
            DateTime.UtcNow);

        var changedBlobSystem = CreateBlobSystem(
            CreateBlobs("other blob"));

        var changedDiff = snapshotStore.StoreSnapshotPreview(
            changedBlobSystem,
            Fuzzy.Default);

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
        var snapshotStore = CreateSnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            CreateBlobSystem(CreateBlobs("some blob")),
            Fuzzy.Default,
            DateTime.UtcNow);

        var actualDiff = snapshotStore.MirrorSnapshotPreview(
            CreateBlobSystem(CreateBlobs("other blob")),
            Fuzzy.Default,
            snapshotId);

        Assert.Equal(
            new DiffSet(
                new[] { "some blob" },
                Array.Empty<string>(),
                new[] { "other blob" }),
            actualDiff);
    }

    private static byte[] GenerateRandomNumber(int length)
    {
        using var randomGenerator = RandomNumberGenerator.Create();
        var randomNumber = new byte[length];
        randomGenerator.GetBytes(randomNumber);

        return randomNumber;
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

            for (var i = 0; i < value.Length; i++)
            {
                value[i]++;
            }

            repository.RemoveValue(key);
            repository.StoreValue(key, value);
        }
    }

    private static SnapshotStore CreateSnapshotStore(
        IRepository? repository = null,
        FastCdc? fastCdc = null,
        string password = "secret")
    {
        return new SnapshotStore(
            repository ?? CreateRepository(),
            fastCdc ?? new FastCdc(),
            new Prompt(new[] { new DummyPrompt(password) }),
            new DummyProbe());
    }

    private static IRepository CreateRepository()
    {
        return new MemoryRepository();
    }

    private static Blob[] CreateBlobs(params string[] blobNames)
    {
        return (blobNames.Any() ? blobNames : new[] { "blob1", "blob2" })
            .Select(b => new Blob(b, DateTime.UtcNow))
            .ToArray();
    }

    public static IBlobSystem CreateBlobSystem(
        IEnumerable<Blob>? blobs = null,
        Func<string, byte[]>? generate = null)
    {
        blobs ??= Array.Empty<Blob>();
        generate ??= (blobName => Encoding.UTF8.GetBytes(blobName));

        var blobSystem = new MemoryBlobSystem();

        foreach (var blob in blobs)
        {
            using var stream = blobSystem.NewWrite(blob);

            stream.Write(generate(blob.Name));
        }

        return blobSystem;
    }

    private static byte[] ToBytes(Stream stream)
    {
        using var memoryStream = new MemoryStream();

        stream.CopyTo(memoryStream);

        return memoryStream.ToArray();
    }
}
