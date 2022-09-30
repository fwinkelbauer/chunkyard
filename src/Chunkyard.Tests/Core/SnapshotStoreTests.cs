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
    public static void StoreSnapshot_Does_Not_Deduplicate_Chunks_For_Known_Blobs()
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

        Assert.NotEqual(blobReferences1, blobReferences2);

        Assert.Equal(
            blobReferences1.Select(br => br.Blob.Name),
            blobReferences2.Select(br => br.Blob.Name));

        Assert.NotEqual(
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
            snapshotStore.GetSnapshot(Repository.LatestReferenceId));
    }

    [Fact]
    public static void GetSnapshot_Throws_If_Snapshot_Does_Not_Exist()
    {
        var snapshotStore = Some.SnapshotStore();

        var snapshotId = snapshotStore.StoreSnapshot(
            Some.BlobSystem(Some.Blobs()));

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.GetSnapshot(snapshotId + 1));

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.GetSnapshot(Repository.SecondLatestReferenceId));
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
            snapshotStore.GetSnapshot(Repository.SecondLatestReferenceId));
    }

    [Fact]
    public static void GetSnapshot_Throws_When_Empty()
    {
        var snapshotStore = Some.SnapshotStore();

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.GetSnapshot(Repository.LatestReferenceId));
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

        Remove(repository, repository.ListChunkIds());

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

        Change(repository, repository.ListChunkIds());

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

        Remove(repository, chunkIds);

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

        Change(repository, chunkIds);

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

        var blobSystem = Some.BlobSystem(
            blobs,
            _ => new byte[] { 0xAB, 0xCD, 0xEF });

        var expectedContent = ToContent(blobSystem);

        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);

        blobSystem.RemoveBlob(blobs.First().Name);

        var blobToUpdate = new Blob(
            blobs.Last().Name,
            blobs.Last().LastWriteTimeUtc.AddHours(1));

        Change(blobSystem, blobToUpdate);

        snapshotStore.RestoreSnapshot(
            blobSystem,
            snapshotId,
            Fuzzy.Default);

        Assert.Equal(
            expectedContent,
            ToContent(blobSystem));
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

        Assert.Throws<ChunkyardException>(
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

        Assert.NotEmpty(repository.ListChunkIds());

        snapshotStore.GarbageCollect();

        Assert.Empty(repository.ListChunkIds());
        Assert.Empty(repository.ListReferenceIds());
    }

    [Fact]
    public static void RemoveSnapshot_Removes_Existing_Snapshots()
    {
        var snapshotStore = Some.SnapshotStore();
        var blobSystem = Some.BlobSystem(Some.Blobs());

        var snapshotId = snapshotStore.StoreSnapshot(blobSystem);
        _ = snapshotStore.StoreSnapshot(blobSystem);

        snapshotStore.RemoveSnapshot(snapshotId);
        snapshotStore.RemoveSnapshot(Repository.LatestReferenceId);

        Assert.Empty(snapshotStore.ListSnapshotIds());
    }

    [Fact]
    public static void RemoveSnapshot_Throws_When_Empty()
    {
        var snapshotStore = Some.SnapshotStore();

        Assert.Throws<ChunkyardException>(
            () => snapshotStore.RemoveSnapshot(Repository.LatestReferenceId));
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
            repository.ListChunkIds().OrderBy(k => k),
            otherRepository.ListChunkIds().OrderBy(k => k));

        Assert.Equal(
            repository.ListReferenceIds(),
            otherRepository.ListReferenceIds());
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

        Change(repository, chunkIds);

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

        Change(repository, repository.ListReferenceIds());

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

    private static void Remove(
        Repository repository,
        IEnumerable<string> chunkIds)
    {
        foreach (var chunkId in chunkIds)
        {
            repository.RemoveChunk(chunkId);
        }
    }

    private static void Remove(
        Repository repository,
        IEnumerable<int> referenceIds)
    {
        foreach (var referenceId in referenceIds)
        {
            repository.RemoveReference(referenceId);
        }
    }

    private static void Change(
        Repository repository,
        IEnumerable<string> chunkIds)
    {
        foreach (var chunkId in chunkIds)
        {
            var bytes = repository.RetrieveChunk(chunkId);

            repository.RemoveChunk(chunkId);
            repository.StoreChunkUnsafe(
                chunkId,
                bytes.Concat(new byte[] { 0xFF }).ToArray());
        }
    }

    private static void Change(
        Repository repository,
        IEnumerable<int> referenceIds)
    {
        foreach (var referenceId in referenceIds)
        {
            var bytes = repository.RetrieveReference(referenceId);

            repository.RemoveReference(referenceId);
            repository.StoreReferenceUnsafe(
                referenceId,
                bytes.Concat(new byte[] { 0xFF }).ToArray());
        }
    }

    private static void Change(
        IBlobSystem blobSystem,
        Blob blob)
    {
        using var stream = blobSystem.BlobExists(blob.Name)
            ? blobSystem.OpenRead(blob.Name)
            : new MemoryStream();

        stream.Write(new byte[] { 0xAB, 0xCD });

        using var writeStream = blobSystem.OpenWrite(blob);

        stream.CopyTo(writeStream);
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
