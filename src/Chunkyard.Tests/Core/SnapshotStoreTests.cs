using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
                new DummyBlobSystem(expectedBlobs),
                Fuzzy.ExcludeNothing,
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
        public static void StoreSnapshot_Fuzzy_Excludes_Blobs()
        {
            var snapshotStore = CreateSnapshotStore();

            var blobs = CreateBlobs(new[] { "some blob", "other blob" });

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobSystem(blobs),
                Fuzzy.Exclude(new[] { "other" }),
                DateTime.UtcNow);

            var snapshot = snapshotStore.GetSnapshot(snapshotId);

            Assert.Equal(
                new[] { blobs[0] },
                snapshot.BlobReferences.Select(br => br.ToBlob()));
        }

        [Fact]
        public static void StoreSnapshot_Reuses_Nonce_For_Known_Blobs()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshotId1 = snapshotStore.StoreSnapshot(
                new DummyBlobSystem(
                    CreateBlobs(new[] { "some blob" }),
                    blobName => new byte[] { 0x11, 0x11 }),
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            var snapshotId2 = snapshotStore.StoreSnapshot(
                new DummyBlobSystem(
                    CreateBlobs(new[] { "some blob" }),
                    blobName => new byte[] { 0x22, 0x22 }),
                Fuzzy.ExcludeNothing,
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
        public static void StoreSnapshot_Does_Not_Read_Blob_With_Unchanged_LastWriteTimeUtc()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobs = CreateBlobs();

            var blobSystem1 = new DummyBlobSystem(
                blobs,
                blobName => new byte[] { 0x11, 0x11 });

            var snapshotId1 = snapshotStore.StoreSnapshot(
                blobSystem1,
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            var blobSystem2 = new DummyBlobSystem(
                blobs,
                blobName => new byte[] { 0x22, 0x22 });

            var snapshotId2 = snapshotStore.StoreSnapshot(
                blobSystem2,
                Fuzzy.ExcludeNothing,
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
                new DummyBlobSystem(CreateBlobs(Array.Empty<string>())),
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            Assert.Equal(0, snapshotId);
        }

        [Fact]
        public static void StoreSnapshot_Stores_Empty_Blob_Without_Chunks()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobSystem(
                    CreateBlobs(new[] { "empty file" }),
                    blobName => Array.Empty<byte>()),
                Fuzzy.ExcludeNothing,
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
                    new DummyBlobSystem(expectedBlobs),
                    Fuzzy.ExcludeNothing,
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
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
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
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
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
            var blobReader = new DummyBlobSystem(CreateBlobs());

            var snapshotId = snapshotStore.StoreSnapshot(
                blobReader,
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            var snapshotIdToRemove = snapshotStore.StoreSnapshot(
                blobReader,
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            snapshotStore.StoreSnapshot(
                blobReader,
                Fuzzy.ExcludeNothing,
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
            var checkFuzzy = Fuzzy.IncludeAll;

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
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
            var checkFuzzy = Fuzzy.IncludeAll;

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
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
            var checkFuzzy = Fuzzy.IncludeAll;

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
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
            var checkFuzzy = Fuzzy.IncludeAll;

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
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
            var checkFuzzy = Fuzzy.IncludeAll;

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
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
            var checkFuzzy = Fuzzy.IncludeAll;

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
            var blobReader = new DummyBlobSystem(CreateBlobs());

            Assert.True(snapshotStore.IsEmpty);

            snapshotStore.StoreSnapshot(
                blobReader,
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            snapshotStore.StoreSnapshot(
                blobReader,
                Fuzzy.ExcludeNothing,
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
                new DummyBlobSystem(
                    blobs,
                    _ => expectedBytes),
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            var blobSystem = new DummyBlobSystem();

            var retrievedBlobs = snapshotStore.RetrieveSnapshot(
                blobSystem,
                snapshotId,
                Fuzzy.IncludeAll);

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
                new DummyBlobSystem(blobs),
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            var blobWriter = new DummyBlobSystem(
                new[]
                {
                    blobs[1],
                    new Blob(blobs[2].Name, DateTime.UtcNow)
                },
                blobName => Array.Empty<byte>());

            snapshotStore.RetrieveSnapshot(
                blobWriter,
                snapshotId,
                Fuzzy.IncludeAll);

            foreach (var blob in blobs)
            {
                Assert.True(blobWriter.BlobExists(blob.Name));
                Assert.Equal(blob, blobWriter.FetchMetadata(blob.Name));

                using var stream = blobWriter.OpenRead(blob.Name);
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
                new DummyBlobSystem(
                    blobs,
                    _ => Array.Empty<byte>()),
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            var blobSystem = new DummyBlobSystem();

            var retrievedBlobs = snapshotStore.RetrieveSnapshot(
                blobSystem,
                snapshotId,
                Fuzzy.IncludeAll);

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
                    new DummyBlobSystem(),
                    SnapshotStore.LatestSnapshotId,
                    Fuzzy.IncludeAll));
        }

        [Fact]
        public static void RetrieveSnapshot_Throws_Given_Wrong_Key()
        {
            var snapshotId = CreateSnapshotStore(password: "a").StoreSnapshot(
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            Assert.Throws<ChunkyardException>(
                () => CreateSnapshotStore(password: "b").RetrieveSnapshot(
                    new DummyBlobSystem(),
                    snapshotId,
                    Fuzzy.IncludeAll));
        }

        [Fact]
        public static void ShowSnapshot_Lists_Blobs()
        {
            var snapshotStore = CreateSnapshotStore();
            var expectedBlobs = CreateBlobs();

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobSystem(expectedBlobs),
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            var blobReferences = snapshotStore.ShowSnapshot(
                snapshotId,
                Fuzzy.IncludeAll);

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
                    Fuzzy.IncludeAll));
        }

        [Fact]
        public static void GarbageCollect_Keeps_Used_Uris()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            snapshotStore.GarbageCollect();

            Assert.True(
                snapshotStore.CheckSnapshotValid(
                    snapshotId,
                    Fuzzy.IncludeAll));
        }

        [Fact]
        public static void GarbageCollect_Removes_Unused_Uris()
        {
            var uriRepository = CreateRepository<Uri>();
            var snapshotStore = CreateSnapshotStore(
                uriRepository);

            var snapshotId = snapshotStore.StoreSnapshot(
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            snapshotStore.RemoveSnapshot(snapshotId);
            snapshotStore.GarbageCollect();

            Assert.Empty(uriRepository.ListKeys());
        }

        [Fact]
        public static void RemoveSnapshot_Removes_Existing_Snapshots()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobReader = new DummyBlobSystem(CreateBlobs());

            var snapshotId = snapshotStore.StoreSnapshot(
                blobReader,
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            snapshotStore.StoreSnapshot(
                blobReader,
                Fuzzy.ExcludeNothing,
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
            var blobReader = new DummyBlobSystem(CreateBlobs());

            snapshotStore.StoreSnapshot(
                blobReader,
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            var snapshotId = snapshotStore.StoreSnapshot(
                blobReader,
                Fuzzy.ExcludeNothing,
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
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
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
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            snapshotStore.KeepSnapshots(0);

            Assert.Empty(snapshotStore.GetSnapshots());
        }

        [Fact]
        public static void KeepSnapshots_Negative_Input_Empties_Store()
        {
            var snapshotStore = CreateSnapshotStore();

            snapshotStore.StoreSnapshot(
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
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
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
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
                new DummyBlobSystem(CreateBlobs(new[] { "some blob" })),
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            snapshotStore.StoreSnapshot(
                new DummyBlobSystem(CreateBlobs(new[] { "some new blob" })),
                Fuzzy.ExcludeNothing,
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
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
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
                new DummyBlobSystem(CreateBlobs()),
                Fuzzy.ExcludeNothing,
                DateTime.UtcNow);

            Invalidate(intRepository, intRepository.ListKeys());

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.Copy(
                    CreateRepository<Uri>(),
                    CreateRepository<int>()));
        }

        private static byte[] GenerateRandomNumber(int length)
        {
            using var randomGenerator = RandomNumberGenerator.Create();
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

        private static byte[] ToBytes(Stream stream)
        {
            using var memoryStream = new MemoryStream();

            stream.CopyTo(memoryStream);

            return memoryStream.ToArray();
        }
    }
}
