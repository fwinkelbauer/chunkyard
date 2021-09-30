using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Chunkyard.Core;
using Chunkyard.Tests.Infrastructure;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class SnapshotStoreTests
    {
        [Fact]
        public static void StoreSnapshot_Creates_Snapshot()
        {
            var snapshotStore = CreateSnapshotStore();
            var expectedBlobs = CreateBlobs(new[] { "some blob" });

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobReader(expectedBlobs),
                DateTime.UtcNow);

            var snapshot = snapshotStore.GetSnapshot(snapshotId);

            Assert.Equal(0, snapshotId);

            Assert.Equal(
                new[] { snapshot },
                snapshotStore.GetSnapshots());

            Assert.Equal(
                expectedBlobs,
                snapshot.BlobReferences.Select(br => br.ToBlob()));
        }

        [Fact]
        public static void StoreSnapshot_Reuses_Nonce_For_Known_Blobs()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshotId1 = snapshotStore.StoreSnapshot(
                new DummyBlobReader(
                    CreateBlobs(new[] { "some blob" }),
                    blobName => Encoding.UTF8.GetBytes($"old {blobName}")),
                DateTime.UtcNow);

            var snapshotId2 = snapshotStore.StoreSnapshot(
                new DummyBlobReader(
                    CreateBlobs(new[] { "some blob" }),
                    blobName => Encoding.UTF8.GetBytes($"new {blobName}")),
                DateTime.UtcNow);

            var snapshot1 = snapshotStore.GetSnapshot(snapshotId1);
            var snapshot2 = snapshotStore.GetSnapshot(snapshotId2);

            Assert.NotEqual(
                snapshot1.BlobReferences,
                snapshot2.BlobReferences);

            Assert.Equal(
                snapshot1.BlobReferences.Select(br => br.Name),
                snapshot2.BlobReferences.Select(br => br.Name));

            Assert.Equal(
                snapshot1.BlobReferences.Select(br => br.Nonce),
                snapshot2.BlobReferences.Select(br => br.Nonce));
        }

        [Fact]
        public static void StoreSnapshot_Does_Not_Read_Unchanged_Blob()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobs = CreateBlobs();

            var snapshotId1 = snapshotStore.StoreSnapshot(
                new DummyBlobReader(blobs),
                DateTime.UtcNow);

            var snapshotId2 = snapshotStore.StoreSnapshot(
                new DummyBlobReader(
                    blobs,
                    _ => throw new NotImplementedException()),
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
                new DummyBlobReader(CreateBlobs(Array.Empty<string>())),
                DateTime.UtcNow);

            Assert.Equal(0, snapshotId);
        }

        [Fact]
        public static void New_SnapshotStore_Can_Read_Existing_Snapshot()
        {
            var uriRepository = CreateRepository<Uri>();
            var intRepository = CreateRepository<int>();

            var expectedBlobs = CreateBlobs(new[] { "some blob" });

            var snapshotId = CreateSnapshotStore(uriRepository, intRepository)
                .StoreSnapshot(
                    new DummyBlobReader(expectedBlobs),
                    DateTime.UtcNow);

            var actualBlobs = CreateSnapshotStore(uriRepository, intRepository)
                .GetSnapshot(snapshotId).BlobReferences
                .Select(br => br.ToBlob());

            Assert.Equal(
                expectedBlobs,
                actualBlobs);
        }

        [Fact]
        public static void GetSnapshot_Accepts_Negative_SnapshotIds()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobReader(CreateBlobs()),
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
                new DummyBlobReader(CreateBlobs()),
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
            var blobReader = new DummyBlobReader(CreateBlobs());

            var snapshotId = snapshotStore.StoreSnapshot(
                blobReader,
                DateTime.UtcNow);

            var snapshotIdToRemove = snapshotStore.StoreSnapshot(
                blobReader,
                DateTime.UtcNow);

            snapshotStore.StoreSnapshot(
                blobReader,
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
            var checkFuzzy = Fuzzy.MatchAll;

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobReader(CreateBlobs()),
                DateTime.UtcNow);

            Assert.True(
                snapshotStore.CheckSnapshotExists(
                    snapshotId,
                    checkFuzzy));

            Assert.True(
                snapshotStore.CheckSnapshotValid(
                    snapshotId,
                    checkFuzzy));
        }

        [Fact]
        public static void CheckSnapshot_Throws_If_Snapshot_Missing()
        {
            var uriRepository = CreateRepository<Uri>();
            var snapshotStore = CreateSnapshotStore(uriRepository);
            var checkFuzzy = Fuzzy.MatchAll;

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobReader(CreateBlobs()),
                DateTime.UtcNow);

            foreach (var contentUri in uriRepository.ListKeys())
            {
                uriRepository.RemoveValue(contentUri);
            }

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotExists(
                    snapshotId,
                    checkFuzzy));

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotValid(
                    snapshotId,
                    checkFuzzy));
        }

        [Fact]
        public static void CheckSnapshot_Throws_If_Snapshot_Invalid()
        {
            var uriRepository = CreateRepository<Uri>();
            var snapshotStore = CreateSnapshotStore(uriRepository);
            var checkFuzzy = Fuzzy.MatchAll;

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobReader(CreateBlobs()),
                DateTime.UtcNow);

            Invalidate(uriRepository, uriRepository.ListKeys());

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotExists(
                    snapshotId,
                    checkFuzzy));

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotValid(
                    snapshotId,
                    checkFuzzy));
        }

        [Fact]
        public static void CheckSnapshot_Detects_Missing_Blob()
        {
            var uriRepository = CreateRepository<Uri>();
            var snapshotStore = CreateSnapshotStore(uriRepository);
            var checkFuzzy = Fuzzy.MatchAll;

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobReader(CreateBlobs()),
                DateTime.UtcNow);

            var contentUris = snapshotStore.GetSnapshot(snapshotId)
                .BlobReferences
                .SelectMany(b => b.ContentUris);

            foreach (var contentUri in contentUris)
            {
                uriRepository.RemoveValue(contentUri);
            }

            Assert.False(
                snapshotStore.CheckSnapshotExists(
                    snapshotId,
                    checkFuzzy));

            Assert.False(
                snapshotStore.CheckSnapshotValid(
                    snapshotId,
                    checkFuzzy));
        }

        [Fact]
        public static void CheckSnapshot_Detects_Invalid_Blob()
        {
            var uriRepository = CreateRepository<Uri>();
            var snapshotStore = CreateSnapshotStore(uriRepository);
            var checkFuzzy = Fuzzy.MatchAll;

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobReader(CreateBlobs()),
                DateTime.UtcNow);

            var contentUris = snapshotStore.GetSnapshot(snapshotId)
                .BlobReferences
                .SelectMany(b => b.ContentUris);

            Invalidate(uriRepository, contentUris);

            Assert.True(
               snapshotStore.CheckSnapshotExists(
                   snapshotId,
                   checkFuzzy));

            Assert.False(
                snapshotStore.CheckSnapshotValid(
                    snapshotId,
                    checkFuzzy));
        }

        [Fact]
        public static void CheckSnapshot_Throws_On_Empty_SnapshotStore()
        {
            var snapshotStore = CreateSnapshotStore();
            var checkFuzzy = Fuzzy.MatchAll;

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotExists(
                    SnapshotStore.LatestSnapshotId,
                    checkFuzzy));

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotValid(
                    SnapshotStore.LatestSnapshotId,
                    checkFuzzy));
        }

        [Fact]
        public static void IsEmpty_Respects_StoreSnapshot_And_RemoveSnapshot()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobReader = new DummyBlobReader(CreateBlobs());

            Assert.True(snapshotStore.IsEmpty);

            snapshotStore.StoreSnapshot(
                blobReader,
                DateTime.UtcNow);

            snapshotStore.StoreSnapshot(
                blobReader,
                DateTime.UtcNow);

            Assert.False(snapshotStore.IsEmpty);

            snapshotStore.RemoveSnapshot(SnapshotStore.LatestSnapshotId);

            Assert.False(snapshotStore.IsEmpty);

            snapshotStore.RemoveSnapshot(SnapshotStore.LatestSnapshotId);

            Assert.True(snapshotStore.IsEmpty);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public static void RetrieveSnapshot_Writes_Ordered_Blob_Chunks_To_Stream(
            int chunkCount)
        {
            var snapshotStore = CreateSnapshotStore(
                fastCdc: new FastCdc(64, 256, 1024),
                parallelizeChunkThreshold: chunkCount);

            var blobs = CreateBlobs(new[] { "some blob" });

            // Create data that is large enough to create at least two chunks
            var expectedBytes = GenerateRandomNumber(1025);

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobReader(
                    blobs,
                    _ => expectedBytes),
                DateTime.UtcNow);

            var blobWriter = new DummyBlobWriter();

            var retrievedBlobs = snapshotStore.RetrieveSnapshot(
                blobWriter,
                snapshotId,
                Fuzzy.MatchAll);

            var blobReference = snapshotStore.GetSnapshot(snapshotId)
                .BlobReferences.First();

            using var decryptStream = new MemoryStream();
            snapshotStore.RetrieveContent(
                blobReference.ContentUris,
                decryptStream);

            Assert.True(blobReference.ContentUris.Count > 1);
            Assert.Equal(blobs, retrievedBlobs);
            Assert.Equal(expectedBytes, blobWriter.ShowContent(blobs[0].Name));
            Assert.Equal(expectedBytes, decryptStream.ToArray());
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
                new DummyBlobReader(blobs),
                DateTime.UtcNow);

            var blobWriter = new DummyBlobWriter(
                new[]
                {
                    blobs[1],
                    new Blob(blobs[2].Name, DateTime.UtcNow)
                });

            snapshotStore.RetrieveSnapshot(
                blobWriter,
                snapshotId,
                Fuzzy.MatchAll);

            foreach (var blob in blobs)
            {
                Assert.Equal(blob, blobWriter.FindBlob(blob.Name));
            }

            Assert.NotEmpty(blobWriter.ShowContent(blobs[0].Name));
            Assert.Null(blobWriter.ShowContent(blobs[1].Name));
            Assert.NotEmpty(blobWriter.ShowContent(blobs[2].Name));
        }

        [Fact]
        public static void RetrieveSnapshot_Can_Write_Empty_Blobs()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobs = CreateBlobs(new[] { "some empty blob" });

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobReader(
                    blobs,
                    _ => Array.Empty<byte>()),
                DateTime.UtcNow);

            var blobWriter = new DummyBlobWriter();

            snapshotStore.RetrieveSnapshot(
                blobWriter,
                snapshotId,
                Fuzzy.MatchAll);

            Assert.Empty(blobWriter.ShowContent(blobs[0].Name));
        }

        [Fact]
        public static void RetrieveSnapshot_Throws_On_Empty_SnapshotStore()
        {
            var snapshotStore = CreateSnapshotStore();

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.RetrieveSnapshot(
                    new DummyBlobWriter(),
                    SnapshotStore.LatestSnapshotId,
                    Fuzzy.MatchAll));
        }

        [Fact]
        public static void RetrieveSnapshot_Throws_Given_Wrong_Key()
        {
            var snapshotId = CreateSnapshotStore(password: "a").StoreSnapshot(
                new DummyBlobReader(CreateBlobs()),
                DateTime.UtcNow);

            Assert.Throws<ChunkyardException>(
                () => CreateSnapshotStore(password: "b").RetrieveSnapshot(
                    new DummyBlobWriter(),
                    snapshotId,
                    Fuzzy.MatchAll));
        }

        [Fact]
        public static void ShowSnapshot_Lists_Blobs()
        {
            var snapshotStore = CreateSnapshotStore();
            var expectedBlobs = CreateBlobs();

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobReader(expectedBlobs),
                DateTime.UtcNow);

            var blobReferences = snapshotStore.ShowSnapshot(
                snapshotId,
                Fuzzy.MatchAll);

            Assert.Equal(
                expectedBlobs,
                blobReferences.Select(br => br.ToBlob()));
        }

        [Fact]
        public static void ShowSnapshot_Throws_On_Empty_SnapshotStore()
        {
            var snapshotStore = CreateSnapshotStore();

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.ShowSnapshot(
                    SnapshotStore.LatestSnapshotId,
                    Fuzzy.MatchAll));
        }

        [Fact]
        public static void GarbageCollect_Keeps_Used_Uris()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobReader(CreateBlobs()),
                DateTime.UtcNow);

            snapshotStore.GarbageCollect();

            Assert.True(
                snapshotStore.CheckSnapshotValid(
                    snapshotId,
                    Fuzzy.MatchAll));
        }

        [Fact]
        public static void GarbageCollect_Removes_Unused_Uris()
        {
            var uriRepository = CreateRepository<Uri>();
            var snapshotStore = CreateSnapshotStore(
                uriRepository);

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobReader(CreateBlobs()),
                DateTime.UtcNow);

            snapshotStore.RemoveSnapshot(snapshotId);
            snapshotStore.GarbageCollect();

            Assert.Empty(uriRepository.ListKeys());
        }

        [Fact]
        public static void RemoveSnapshot_Removes_Existing_Snapshots()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobReader = new DummyBlobReader(CreateBlobs());

            var snapshotId = snapshotStore.StoreSnapshot(
                blobReader,
                DateTime.UtcNow);

            snapshotStore.StoreSnapshot(
                blobReader,
                DateTime.UtcNow);

            snapshotStore.RemoveSnapshot(snapshotId);
            snapshotStore.RemoveSnapshot(SnapshotStore.LatestSnapshotId);

            Assert.Empty(snapshotStore.GetSnapshots());
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
        public static void KeepSnapshots_Deletes_Previous_Snapshots()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobReader = new DummyBlobReader(CreateBlobs());

            snapshotStore.StoreSnapshot(
                blobReader,
                DateTime.UtcNow);

            var snapshotId = snapshotStore.StoreSnapshot(
                blobReader,
                DateTime.UtcNow);

            snapshotStore.KeepSnapshots(1);

            Assert.Equal(
                new[] { snapshotId },
                snapshotStore.GetSnapshots().Select(s => s.SnapshotId));
        }

        [Fact]
        public static void KeepSnapshots_Does_Nothing_If_Equals_Or_Greater_Than_Current()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobReader(CreateBlobs()),
                DateTime.UtcNow);

            snapshotStore.KeepSnapshots(1);
            snapshotStore.KeepSnapshots(2);

            Assert.Equal(
                new[] { snapshotId },
                snapshotStore.GetSnapshots().Select(s => s.SnapshotId));
        }

        [Fact]
        public static void KeepSnapshots_Zero_Input_Empties_Store()
        {
            var snapshotStore = CreateSnapshotStore();

            snapshotStore.StoreSnapshot(
                new DummyBlobReader(CreateBlobs()),
                DateTime.UtcNow);

            snapshotStore.KeepSnapshots(0);

            Assert.Empty(snapshotStore.GetSnapshots());
        }

        [Fact]
        public static void KeepSnapshots_Negative_Input_Empties_Store()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobReader(CreateBlobs()),
                DateTime.UtcNow);

            snapshotStore.KeepSnapshots(-1);

            Assert.Empty(snapshotStore.GetSnapshots());
        }

        [Fact]
        public static void Copy_Copies_Everything()
        {
            var sourceUriRepository = CreateRepository<Uri>();
            var sourceIntRepository = CreateRepository<int>();
            var snapshotStore = CreateSnapshotStore(
                sourceUriRepository,
                sourceIntRepository);

            snapshotStore.StoreSnapshot(
                new DummyBlobReader(CreateBlobs()),
                DateTime.UtcNow);

            var destinationUriRepository = CreateRepository<Uri>();
            var destinationIntRepository = CreateRepository<int>();

            snapshotStore.Copy(
                destinationUriRepository,
                destinationIntRepository);

            Assert.Equal(
                sourceUriRepository.ListKeys(),
                destinationUriRepository.ListKeys());

            Assert.Equal(
                sourceIntRepository.ListKeys(),
                destinationIntRepository.ListKeys());
        }

        [Fact]
        public static void Mirror_Copies_New_Data_And_Deletes_Old_Data()
        {
            var sourceUriRepository = CreateRepository<Uri>();
            var sourceIntRepository = CreateRepository<int>();
            var snapshotStore = CreateSnapshotStore(
                sourceUriRepository,
                sourceIntRepository);

            var firstSnapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobReader(CreateBlobs(new[] { "some blob" })),
                DateTime.UtcNow);

            snapshotStore.StoreSnapshot(
                new DummyBlobReader(CreateBlobs(new[] { "some new blob" })),
                DateTime.UtcNow);

            var destinationUriRepository = CreateRepository<Uri>();
            var destinationIntRepository = CreateRepository<int>();

            snapshotStore.Mirror(
                destinationUriRepository,
                destinationIntRepository);

            snapshotStore.RemoveSnapshot(firstSnapshotId);
            snapshotStore.GarbageCollect();

            snapshotStore.Mirror(
                destinationUriRepository,
                destinationIntRepository);

            Assert.Equal(
                sourceUriRepository.ListKeys(),
                destinationUriRepository.ListKeys());

            Assert.Equal(
                sourceIntRepository.ListKeys(),
                destinationIntRepository.ListKeys());
        }

        [Fact]
        public static void Copy_Throws_On_Invalid_Content()
        {
            var uriRepository = CreateRepository<Uri>();
            var snapshotStore = CreateSnapshotStore(
                uriRepository,
                CreateRepository<int>());

            snapshotStore.StoreSnapshot(
                new DummyBlobReader(CreateBlobs()),
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
                new DummyBlobReader(CreateBlobs()),
                DateTime.UtcNow);

            Invalidate(intRepository, intRepository.ListKeys());

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.Copy(
                    CreateRepository<Uri>(),
                    CreateRepository<int>()));
        }

        private static byte[] GenerateRandomNumber(int length)
        {
            using var randomGenerator = new RNGCryptoServiceProvider();
            var randomNumber = new byte[length];
            randomGenerator.GetBytes(randomNumber);

            return randomNumber;
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
            string password = "secret",
            int parallelizeChunkThreshold = 100)
        {
            return new SnapshotStore(
                uriRepository ?? CreateRepository<Uri>(),
                intRepository ?? CreateRepository<int>(),
                fastCdc ?? new FastCdc(),
                new DummyPrompt(password),
                new DummyProbe(),
                parallelizeChunkThreshold);
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
    }
}
