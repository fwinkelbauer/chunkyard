namespace Chunkyard.Tests.Core;

public static class SnapshotStoreTests
{
    [Fact]
    public static void StoreSnapshot_Creates_Snapshot()
    {
        var snapshotStore = CreateSnapshotStore();
        var expectedBlobs = CreateBlobs(new[] { "some blob" });

        var snapshotId = snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(expectedBlobs),
            Fuzzy.Default,
            DateTime.UtcNow);

        var snapshot = snapshotStore.GetSnapshot(snapshotId);

        Assert.Equal(
            new[] { snapshotId },
            snapshotStore.ListSnapshotIds());

        Assert.Equal(
            new[] { snapshot },
            snapshotStore.ListSnapshotIds().Select(snapshotStore.GetSnapshot));

        Assert.Equal(
            expectedBlobs,
            snapshot.BlobReferences.Select(br => br.Blob));
    }

    [Fact]
    public static void StoreSnapshot_Fuzzy_Excludes_Blobs()
    {
        var snapshotStore = CreateSnapshotStore();

        var blobs = CreateBlobs(new[] { "some blob", "other blob" });

        var snapshotId = snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(blobs),
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
            new MemoryBlobSystem(
                CreateBlobs(new[] { "some blob" })),
            Fuzzy.Default,
            DateTime.UtcNow);

        var snapshotId2 = snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(
                CreateBlobs(new[] { "some blob" })),
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
            blobReferences1.SelectMany(br => br.ContentUris),
            blobReferences2.SelectMany(br => br.ContentUris));
    }

    [Fact]
    public static void StoreSnapshot_Does_Not_Read_Blob_With_Unchanged_LastWriteTimeUtc()
    {
        var snapshotStore = CreateSnapshotStore();
        var blobs = CreateBlobs();

        var blobSystem1 = new MemoryBlobSystem(
            blobs,
            blobName => new byte[] { 0x11, 0x11 });

        var snapshotId1 = snapshotStore.StoreSnapshot(
            blobSystem1,
            Fuzzy.Default,
            DateTime.UtcNow);

        var blobSystem2 = new MemoryBlobSystem(
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
            new MemoryBlobSystem(CreateBlobs(Array.Empty<string>())),
            Fuzzy.Default,
            DateTime.UtcNow);

        Assert.Equal(0, snapshotId);
    }

    [Fact]
    public static void StoreSnapshot_Stores_Empty_Blob_Without_Chunks()
    {
        var snapshotStore = CreateSnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(
                CreateBlobs(new[] { "empty file" }),
                blobName => Array.Empty<byte>()),
            Fuzzy.Default,
            DateTime.UtcNow);

        var contentUris = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences
            .First()
            .ContentUris;

        Assert.Empty(contentUris);
    }

    [Fact]
    public static void New_SnapshotStore_Can_Read_Existing_Snapshot()
    {
        var uriRepository = CreateRepository<Uri>();
        var intRepository = CreateRepository<int>();

        var expectedBlobs = CreateBlobs(new[] { "some blob" });

        var snapshotId = CreateSnapshotStore(uriRepository, intRepository)
            .StoreSnapshot(
                new MemoryBlobSystem(expectedBlobs),
                Fuzzy.Default,
                DateTime.UtcNow);

        var actualBlobs = CreateSnapshotStore(uriRepository, intRepository)
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
            new MemoryBlobSystem(CreateBlobs()),
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
            new MemoryBlobSystem(CreateBlobs()),
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
        var blobSystem = new MemoryBlobSystem(CreateBlobs());

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
            new MemoryBlobSystem(CreateBlobs()),
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
        var uriRepository = CreateRepository<Uri>();
        var snapshotStore = CreateSnapshotStore(uriRepository);

        var snapshotId = snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        Remove(uriRepository, uriRepository.ListKeys());

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
        var uriRepository = CreateRepository<Uri>();
        var snapshotStore = CreateSnapshotStore(uriRepository);

        var snapshotId = snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        Invalidate(uriRepository, uriRepository.ListKeys());

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
        var uriRepository = CreateRepository<Uri>();
        var snapshotStore = CreateSnapshotStore(uriRepository);

        var snapshotId = snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        var contentUris = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences
            .SelectMany(b => b.ContentUris);

        Remove(uriRepository, contentUris);

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
        var uriRepository = CreateRepository<Uri>();
        var snapshotStore = CreateSnapshotStore(uriRepository);

        var snapshotId = snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        var contentUris = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences
            .SelectMany(b => b.ContentUris);

        Invalidate(uriRepository, contentUris);

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
    public static void IsEmpty_Respects_StoreSnapshot_And_RemoveSnapshot()
    {
        var snapshotStore = CreateSnapshotStore();
        var blobSystem = new MemoryBlobSystem(CreateBlobs());

        Assert.True(snapshotStore.IsEmpty);

        snapshotStore.StoreSnapshot(
            blobSystem,
            Fuzzy.Default,
            DateTime.UtcNow);

        snapshotStore.StoreSnapshot(
            blobSystem,
            Fuzzy.Default,
            DateTime.UtcNow);

        Assert.False(snapshotStore.IsEmpty);

        snapshotStore.RemoveSnapshot(SnapshotStore.LatestSnapshotId);

        Assert.False(snapshotStore.IsEmpty);

        snapshotStore.RemoveSnapshot(SnapshotStore.LatestSnapshotId);

        Assert.True(snapshotStore.IsEmpty);
    }

    [Fact]
    public static void RetrieveSnapshot_Writes_Ordered_Blob_Chunks_To_Stream()
    {
        var fastCdc = new FastCdc(64, 256, 1024);
        var snapshotStore = CreateSnapshotStore(
            fastCdc: fastCdc);

        var blobs = CreateBlobs(new[] { "some blob" });

        // Create data that is large enough to create at least two chunks
        var expectedBytes = GenerateRandomNumber(fastCdc.MaxSize + 1);

        var snapshotId = snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(
                blobs,
                _ => expectedBytes),
            Fuzzy.Default,
            DateTime.UtcNow);

        var blobSystem = new MemoryBlobSystem();

        var retrievedBlobs = snapshotStore.RetrieveSnapshot(
            blobSystem,
            snapshotId,
            Fuzzy.Default);

        var blobReferences = snapshotStore.GetSnapshot(snapshotId)
            .BlobReferences;

        Assert.Equal(blobs, retrievedBlobs);
        Assert.NotEmpty(blobReferences);

        foreach (var blobReference in blobReferences)
        {
            using var blobStream = blobSystem.OpenRead(blobs[0].Name);
            using var contentStream = new MemoryStream();

            snapshotStore.RetrieveContent(
                blobReference.ContentUris,
                contentStream);

            Assert.True(blobReference.ContentUris.Count > 1);
            Assert.Equal(expectedBytes, ToBytes(blobStream));
            Assert.Equal(expectedBytes, contentStream.ToArray());
        }
    }

    [Fact]
    public static void RetrieveSnapshot_Overwrites_Blobs_If_LastWriteTimeUtc_Differs()
    {
        var snapshotStore = CreateSnapshotStore();

        var blobs = CreateBlobs(
            new[]
            {
                "new blob",
                "unchanged blob",
                "changed blob"
            });

        var snapshotId = snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(blobs),
            Fuzzy.Default,
            DateTime.UtcNow);

        var blobSystem = new MemoryBlobSystem(
            new[]
            {
                blobs[1],
                new Blob(blobs[2].Name, DateTime.UtcNow)
            },
            blobName => Array.Empty<byte>());

        snapshotStore.RetrieveSnapshot(
            blobSystem,
            snapshotId,
            Fuzzy.Default);

        foreach (var blob in blobs)
        {
            Assert.True(blobSystem.BlobExists(blob.Name));
            Assert.Equal(blob, blobSystem.GetBlob(blob.Name));

            using var stream = blobSystem.OpenRead(blob.Name);
            var bytes = ToBytes(stream);

            if (blob.Name.Equals(blobs[1].Name))
            {
                Assert.Empty(bytes);
            }
            else
            {
                Assert.NotEmpty(bytes);
            }
        }
    }

    [Fact]
    public static void RetrieveSnapshot_Can_Write_Empty_Blobs()
    {
        var snapshotStore = CreateSnapshotStore();
        var blobs = CreateBlobs(new[] { "some empty blob" });

        var snapshotId = snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(
                blobs,
                _ => Array.Empty<byte>()),
            Fuzzy.Default,
            DateTime.UtcNow);

        var blobSystem = new MemoryBlobSystem();

        var retrievedBlobs = snapshotStore.RetrieveSnapshot(
            blobSystem,
            snapshotId,
            Fuzzy.Default);

        Assert.Equal(blobs, retrievedBlobs);

        foreach (var blob in retrievedBlobs)
        {
            using var stream = blobSystem.OpenRead(blob.Name);

            Assert.Empty(ToBytes(stream));
        }
    }

    [Fact]
    public static void RetrieveSnapshot_Throws_On_Empty_SnapshotStore()
    {
        var snapshotStore = CreateSnapshotStore();

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.RetrieveSnapshot(
                new MemoryBlobSystem(),
                SnapshotStore.LatestSnapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void RetrieveSnapshot_Throws_Given_Wrong_Key()
    {
        var snapshotId = CreateSnapshotStore(password: "a").StoreSnapshot(
            new MemoryBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        Assert.Throws<ChunkyardException>(
            () => CreateSnapshotStore(password: "b").RetrieveSnapshot(
                new MemoryBlobSystem(),
                snapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void ShowSnapshot_Lists_Blobs()
    {
        var snapshotStore = CreateSnapshotStore();
        var expectedBlobs = CreateBlobs();

        var snapshotId = snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(expectedBlobs),
            Fuzzy.Default,
            DateTime.UtcNow);

        var blobReferences = snapshotStore.ShowSnapshot(
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
            () => snapshotStore.ShowSnapshot(
                SnapshotStore.LatestSnapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void GarbageCollect_Keeps_Used_Uris()
    {
        var snapshotStore = CreateSnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        Assert.Empty(snapshotStore.GarbageCollect());

        Assert.True(
            snapshotStore.CheckSnapshotValid(
                snapshotId,
                Fuzzy.Default));
    }

    [Fact]
    public static void GarbageCollect_Removes_Unused_Uris()
    {
        var uriRepository = CreateRepository<Uri>();
        var snapshotStore = CreateSnapshotStore(
            uriRepository);

        var snapshotId = snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        snapshotStore.RemoveSnapshot(snapshotId);
        snapshotStore.GarbageCollect();

        Assert.Empty(uriRepository.ListKeys());
    }

    [Fact]
    public static void RemoveSnapshot_Removes_Existing_Snapshots()
    {
        var snapshotStore = CreateSnapshotStore();
        var blobSystem = new MemoryBlobSystem(CreateBlobs());

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
        var blobSystem = new MemoryBlobSystem(CreateBlobs());

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
            new MemoryBlobSystem(CreateBlobs()),
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
            new MemoryBlobSystem(CreateBlobs()),
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
            new MemoryBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        snapshotStore.KeepSnapshots(-1);

        Assert.Empty(snapshotStore.ListSnapshotIds());
    }

    [Fact]
    public static void Copy_Copies_Newer_Snapshots()
    {
        var sourceUriRepository = CreateRepository<Uri>();
        var sourceIntRepository = CreateRepository<int>();
        var snapshotStore = CreateSnapshotStore(
            sourceUriRepository,
            sourceIntRepository);

        var destinationUriRepository = CreateRepository<Uri>();
        var destinationIntRepository = CreateRepository<int>();

        for (var i = 0; i < 3; i++)
        {
            snapshotStore.StoreSnapshot(
                new MemoryBlobSystem(CreateBlobs()),
                Fuzzy.Default,
                DateTime.UtcNow);

            snapshotStore.Copy(
                destinationUriRepository,
                destinationIntRepository);

            Assert.Equal(
                sourceUriRepository.ListKeys().OrderBy(u => u.AbsoluteUri),
                destinationUriRepository.ListKeys().OrderBy(u => u.AbsoluteUri));

            Assert.Equal(
                sourceIntRepository.ListKeys(),
                destinationIntRepository.ListKeys());
        }
    }

    [Fact]
    public static void Copy_Throws_On_Invalid_Content()
    {
        var uriRepository = CreateRepository<Uri>();
        var snapshotStore = CreateSnapshotStore(
            uriRepository,
            CreateRepository<int>());

        snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        Invalidate(uriRepository, uriRepository.ListKeys());

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.Copy(
                CreateRepository<Uri>(),
                CreateRepository<int>()));
    }

    [Fact]
    public static void Copy_Throws_On_Invalid_References()
    {
        var intRepository = CreateRepository<int>();
        var snapshotStore = CreateSnapshotStore(
            CreateRepository<Uri>(),
            intRepository);

        snapshotStore.StoreSnapshot(
            new MemoryBlobSystem(CreateBlobs()),
            Fuzzy.Default,
            DateTime.UtcNow);

        Invalidate(intRepository, intRepository.ListKeys());

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.Copy(
                CreateRepository<Uri>(),
                CreateRepository<int>()));
    }

    [Fact]
    public static void Clean_Removes_Blobs_Not_In_Snapshot_Using_Blob_Name_Only()
    {
        var snapshotStore = CreateSnapshotStore();
        var expectedBlobs = CreateBlobs(new[] { "some blob", "other blob" });
        var blobSystem = new MemoryBlobSystem(expectedBlobs);

        var snapshotId = snapshotStore.StoreSnapshot(
            blobSystem,
            Fuzzy.Default,
            DateTime.UtcNow);

        var newBlob = new Blob("blob to delete", DateTime.UtcNow);
        var existingBlob = new Blob("other blob", DateTime.UtcNow);

        using (var writeStream = blobSystem.OpenWrite(newBlob))
        {
            writeStream.Write(new byte[] { 0x10, 0x11 });
        }

        using (var writeStream = blobSystem.OpenWrite(existingBlob))
        {
            writeStream.Write(new byte[] { 0x10, 0x11 });
        }

        snapshotStore.CleanBlobSystem(
            blobSystem,
            Fuzzy.Default,
            snapshotId);

        Assert.Equal(
            expectedBlobs.Select(b => b.Name),
            blobSystem.ListBlobs(Fuzzy.Default).Select(b => b.Name));
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
        IRepository<Uri>? uriRepository = null,
        IRepository<int>? intRepository = null,
        FastCdc? fastCdc = null,
        string password = "secret")
    {
        return new SnapshotStore(
            uriRepository ?? CreateRepository<Uri>(),
            intRepository ?? CreateRepository<int>(),
            fastCdc ?? new FastCdc(),
            new DummyPrompt(password),
            new DummyProbe());
    }

    private static IRepository<T> CreateRepository<T>()
        where T : notnull
    {
        return new MemoryRepository<T>();
    }

    private static Blob[] CreateBlobs(
        string[]? blobNames = null)
    {
        return (blobNames ?? new[] { "blob1", "blob2" })
            .Select(b => new Blob(b, DateTime.UtcNow))
            .ToArray();
    }

    private static byte[] ToBytes(Stream stream)
    {
        using var memoryStream = new MemoryStream();

        stream.CopyTo(memoryStream);

        return memoryStream.ToArray();
    }
}
