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
            var expectedNames = new[] { "some blob" };

            var snapshotId = snapshotStore.StoreSnapshot(
                CreateBlobs(expectedNames),
                Fuzzy.MatchAll,
                DateTime.UtcNow,
                OpenRead);

            var snapshot = snapshotStore.GetSnapshot(snapshotId);

            Assert.Equal(0, snapshotId);

            Assert.Equal(
                new[] { snapshot },
                snapshotStore.GetSnapshots());

            Assert.Equal(
                expectedNames,
                snapshot.BlobReferences.Select(br => br.Name));
        }

        [Fact]
        public static void StoreSnapshot_Reuses_Nonce_For_Known_Blobs()
        {
            var snapshotStore = CreateSnapshotStore();
            var names = new[] { "some blob" };

            var snapshotId1 = snapshotStore.StoreSnapshot(
                CreateBlobs(names),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                blobName => OpenRead($"old {blobName}"));

            var snapshotId2 = snapshotStore.StoreSnapshot(
                CreateBlobs(names),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                blobName => OpenRead($"new {blobName}"));

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
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            var snapshotId2 = snapshotStore.StoreSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                _ => throw new NotImplementedException());

            var snapshot1 = snapshotStore.GetSnapshot(snapshotId1);
            var snapshot2 = snapshotStore.GetSnapshot(snapshotId2);

            Assert.Equal(
                snapshot1.BlobReferences,
                snapshot2.BlobReferences);
        }

        [Fact]
        public static void StoreSnapshot_Does_Read_Unchanged_Blob_When_Asked_To()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobs = CreateBlobs();

            var snapshotId1 = snapshotStore.StoreSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                blobName => OpenRead($"old {blobName}"));

            var snapshotId2 = snapshotStore.StoreSnapshot(
                blobs,
                Fuzzy.MatchAll,
                DateTime.UtcNow,
                blobName => OpenRead($"new {blobName}"));

            var snapshot1 = snapshotStore.GetSnapshot(snapshotId1);
            var snapshot2 = snapshotStore.GetSnapshot(snapshotId2);

            Assert.NotEqual(
                snapshot1.BlobReferences,
                snapshot2.BlobReferences);
        }

        [Fact]
        public static void StoreSnapshot_Creates_Snapshot_Without_Any_Data()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshotId = snapshotStore.StoreSnapshot(
                CreateBlobs(Array.Empty<string>()),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            Assert.Equal(0, snapshotId);
        }

        [Fact]
        public static void New_SnapshotStore_Can_Read_Existing_Snapshot()
        {
            var uriRepository = CreateRepository<Uri>();
            var intRepository = CreateRepository<int>();

            var expectedNames = new[] { "some blob" };

            var snapshotId = CreateSnapshotStore(uriRepository, intRepository)
                .StoreSnapshot(
                    CreateBlobs(expectedNames),
                    Fuzzy.MatchNothing,
                    DateTime.UtcNow,
                    OpenRead);

            var actualNames = CreateSnapshotStore(uriRepository, intRepository)
                .GetSnapshot(snapshotId).BlobReferences
                .Select(br => br.Name);

            Assert.Equal(
                expectedNames,
                actualNames);
        }

        [Fact]
        public static void GetSnapshot_Accepts_Negative_SnapshotIds()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshotId = snapshotStore.StoreSnapshot(
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            Assert.Equal(
                snapshotStore.GetSnapshot(snapshotId),
                snapshotStore.GetSnapshot(SnapshotStore.LatestSnapshotId));
        }

        [Fact]
        public static void GetSnapshot_Throws_If_Version_Does_Not_Exist()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshotId = snapshotStore.StoreSnapshot(
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

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

            var snapshotId = snapshotStore.StoreSnapshot(
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            var snapshotIdToRemove = snapshotStore.StoreSnapshot(
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            snapshotStore.StoreSnapshot(
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

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
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

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
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

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
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

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
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

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
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

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
        public static void IsEmpty_Detects_Store_And_Remove()
        {
            var snapshotStore = CreateSnapshotStore();

            Assert.True(snapshotStore.IsEmpty);

            snapshotStore.StoreSnapshot(
                CreateBlobs(),
                Fuzzy.MatchAll,
                DateTime.UtcNow,
                OpenRead);

            snapshotStore.StoreSnapshot(
                CreateBlobs(),
                Fuzzy.MatchAll,
                DateTime.UtcNow,
                OpenRead);

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
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                _ => new MemoryStream(expectedBytes));

            using var retrieveStream = new MemoryStream();

            var retrievedBlobs = snapshotStore.RetrieveSnapshot(
                snapshotId,
                Fuzzy.MatchAll,
                _ => retrieveStream);

            var blobReference = snapshotStore.GetSnapshot(snapshotId)
                .BlobReferences.First();

            using var decryptStream = new MemoryStream();
            snapshotStore.RetrieveContent(
                blobReference.ContentUris,
                decryptStream);

            Assert.True(blobReference.ContentUris.Count > 1);
            Assert.Equal(blobs, retrievedBlobs);
            Assert.Equal(expectedBytes, retrieveStream.ToArray());
            Assert.Equal(expectedBytes, decryptStream.ToArray());
        }

        [Fact]
        public static void RetrieveSnapshot_Can_Write_Empty_Files()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshotId = snapshotStore.StoreSnapshot(
                CreateBlobs(new[] { "some empty blob" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                _ => new MemoryStream());

            using var retrieveStream = new MemoryStream();
            var restoreCalled = false;

            snapshotStore.RetrieveSnapshot(
                snapshotId,
                Fuzzy.MatchAll,
                _ =>
                {
                    restoreCalled = true;
                    return retrieveStream;
                });

            Assert.True(restoreCalled);
            Assert.Empty(retrieveStream.ToArray());
        }

        [Fact]
        public static void RetrieveSnapshot_Throws_On_Empty_SnapshotStore()
        {
            var snapshotStore = CreateSnapshotStore();

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.RetrieveSnapshot(
                    SnapshotStore.LatestSnapshotId,
                    Fuzzy.MatchAll,
                    _ => new MemoryStream()));
        }

        [Fact]
        public static void RetrieveSnapshot_Throws_Given_Wrong_Key()
        {
            var snapshotId = CreateSnapshotStore(password: "a").StoreSnapshot(
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            Assert.Throws<ChunkyardException>(
                () => CreateSnapshotStore(password: "b").RetrieveSnapshot(
                    snapshotId,
                    Fuzzy.MatchAll,
                    _ => new MemoryStream()));
        }

        [Fact]
        public static void ShowSnapshot_Lists_Blobs()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobs = CreateBlobs();

            var snapshotId = snapshotStore.StoreSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            var blobReferences = snapshotStore.ShowSnapshot(
                snapshotId,
                Fuzzy.MatchAll);

            Assert.Equal(
                blobs,
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
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

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
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            snapshotStore.RemoveSnapshot(snapshotId);
            snapshotStore.GarbageCollect();

            Assert.Empty(uriRepository.ListKeys());
        }

        [Fact]
        public static void RemoveSnapshot_Removes_Existing_Snapshots()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobs = CreateBlobs();

            var snapshotId = snapshotStore.StoreSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            snapshotStore.StoreSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

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
            var blobs = CreateBlobs();

            snapshotStore.StoreSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            var snapshotId = snapshotStore.StoreSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

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
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

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
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            snapshotStore.KeepSnapshots(0);

            Assert.Empty(snapshotStore.GetSnapshots());
        }

        [Fact]
        public static void KeepSnapshots_Negative_Input_Empties_Store()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshotId = snapshotStore.StoreSnapshot(
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

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
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

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
        public static void Copy_Throws_On_Invalid_Content()
        {
            var uriRepository = CreateRepository<Uri>();
            var snapshotStore = CreateSnapshotStore(
                uriRepository,
                CreateRepository<int>());

            snapshotStore.StoreSnapshot(
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

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
                CreateBlobs(),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

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

        private static Blob[] CreateBlobs(
            IEnumerable<string>? names = null)
        {
            return (names ?? new[] { "blob1", "blob2" })
                .Select(n => new Blob(n, DateTime.UtcNow))
                .ToArray();
        }

        private static Stream OpenRead(string blobName)
        {
            return new MemoryStream(
                Encoding.UTF8.GetBytes(blobName));
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
                Id.AlgorithmSha256,
                new DummyPrompt(password),
                new DummyProbe(),
                parallelizeChunkThreshold);
        }

        private static IRepository<T> CreateRepository<T>()
            where T : notnull
        {
            return new MemoryRepository<T>();
        }
    }
}
