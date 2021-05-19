using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Chunkyard.Core;
using Chunkyard.Tests.Infrastructure;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class SnapshotStoreTests
    {
        [Fact]
        public static void AppendSnapshot_Creates_Snapshot()
        {
            var snapshotStore = CreateSnapshotStore();
            var expectedNames = new[] { "some content" };

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(expectedNames),
                Fuzzy.MatchAll,
                DateTime.UtcNow,
                OpenRead);

            Assert.Equal(0, snapshot.SnapshotId);

            Assert.Equal(
                snapshot,
                snapshotStore.GetSnapshot(snapshot.SnapshotId));

            Assert.Equal(
                new[] { snapshot },
                snapshotStore.GetSnapshots());

            Assert.Equal(
                expectedNames,
                snapshot.BlobReferences.Select(c => c.Name));
        }

        [Fact]
        public static void AppendSnapshot_Reuses_Nonce_For_Known_Blobs()
        {
            var snapshotStore = CreateSnapshotStore();
            var names = new[] { "some content" };

            var snapshot1 = snapshotStore.AppendSnapshot(
                CreateBlobs(names),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                blobName => OpenRead($"old {blobName}"));

            var snapshot2 = snapshotStore.AppendSnapshot(
                CreateBlobs(names),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                blobName => OpenRead($"new {blobName}"));

            Assert.NotEqual(
                snapshot1.BlobReferences,
                snapshot2.BlobReferences);

            Assert.Equal(
                snapshot1.BlobReferences.Select(b => b.Name),
                snapshot2.BlobReferences.Select(b => b.Name));

            Assert.Equal(
                snapshot1.BlobReferences.Select(b => b.Nonce),
                snapshot2.BlobReferences.Select(b => b.Nonce));
        }

        [Fact]
        public static void AppendSnapshot_Does_Not_Read_Unchanged_Content()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobs = CreateBlobs(new[] { "some content" });

            var snapshot1 = snapshotStore.AppendSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            var snapshot2 = snapshotStore.AppendSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                _ => throw new NotImplementedException());

            Assert.Equal(
                snapshot1.BlobReferences,
                snapshot2.BlobReferences);
        }

        [Fact]
        public static void AppendSnapshot_Does_Read_Unchanged_Content_When_Asked_To()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobs = CreateBlobs(new[] { "some content" });

            var snapshot1 = snapshotStore.AppendSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                blobName => OpenRead($"old {blobName}"));

            var snapshot2 = snapshotStore.AppendSnapshot(
                blobs,
                Fuzzy.MatchAll,
                DateTime.UtcNow,
                blobName => OpenRead($"new {blobName}"));

            Assert.NotEqual(
                snapshot1.BlobReferences,
                snapshot2.BlobReferences);
        }

        [Fact]
        public static void AppendSnapshot_Creates_Snapshot_Even_If_Empty()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(Array.Empty<string>()),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            Assert.Equal(0, snapshot.SnapshotId);
        }

        [Fact]
        public static void AppendSnapshot_Creates_Snapshot_Without_New_Data()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobs = CreateBlobs(new[] { "some content" });

            snapshotStore.AppendSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            var snapshot = snapshotStore.AppendSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            Assert.Equal(1, snapshot.SnapshotId);
        }

        [Fact]
        public static void New_SnapshotStore_Can_Read_Existing_Snapshot()
        {
            var uriRepository = CreateUriRepository();
            var intRepository = CreateIntRepository();

            var expectedNames = new[] { "some content" };

            var snapshotId = CreateSnapshotStore(uriRepository, intRepository)
                .AppendSnapshot(
                    CreateBlobs(expectedNames),
                    Fuzzy.MatchNothing,
                    DateTime.UtcNow,
                    OpenRead)
                .SnapshotId;

            var actualNames = CreateSnapshotStore(uriRepository, intRepository)
                .GetSnapshot(snapshotId).BlobReferences
                .Select(c => c.Name);

            Assert.Equal(
                expectedNames,
                actualNames);
        }

        [Fact]
        public static void GetSnapshot_Accepts_Negative_SnapshotIds()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            Assert.Equal(
                snapshot,
                snapshotStore.GetSnapshot(SnapshotStore.LatestSnapshotId));
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
        public static void CheckSnapshot_Detects_Ok()
        {
            var snapshotStore = CreateSnapshotStore();
            var checkFuzzy = Fuzzy.MatchAll;

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            Assert.True(
                snapshotStore.CheckSnapshotExists(
                    snapshot.SnapshotId,
                    checkFuzzy));

            Assert.True(
                snapshotStore.CheckSnapshotValid(
                    snapshot.SnapshotId,
                    checkFuzzy));
        }

        [Fact]
        public static void CheckSnapshot_Throws_If_Snapshot_Missing()
        {
            var uriRepository = CreateUriRepository();
            var snapshotStore = CreateSnapshotStore(uriRepository);
            var checkFuzzy = Fuzzy.MatchAll;

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            uriRepository.RemoveValues(uriRepository.ListKeys());

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotExists(
                    snapshot.SnapshotId,
                    checkFuzzy));

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotValid(
                    snapshot.SnapshotId,
                    checkFuzzy));
        }

        [Fact]
        public static void CheckSnapshot_Throws_If_Snapshot_Invalid()
        {
            var uriRepository = CreateUriRepository();
            var snapshotStore = CreateSnapshotStore(uriRepository);
            var checkFuzzy = Fuzzy.MatchAll;

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            uriRepository.InvalidateValues(uriRepository.ListKeys());

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotExists(
                    snapshot.SnapshotId,
                    checkFuzzy));

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotValid(
                    snapshot.SnapshotId,
                    checkFuzzy));
        }

        [Fact]
        public static void CheckSnapshot_Detects_Missing_Content()
        {
            var uriRepository = CreateUriRepository();
            var snapshotStore = CreateSnapshotStore(uriRepository);
            var checkFuzzy = Fuzzy.MatchAll;

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            var contentUris = snapshot.BlobReferences
                .SelectMany(b => b.ContentUris);

            uriRepository.RemoveValues(contentUris);

            Assert.False(
                snapshotStore.CheckSnapshotExists(
                    snapshot.SnapshotId,
                    checkFuzzy));

            Assert.False(
                snapshotStore.CheckSnapshotValid(
                    snapshot.SnapshotId,
                    checkFuzzy));
        }

        [Fact]
        public static void CheckSnapshot_Detects_Invalid_Content()
        {
            var uriRepository = CreateUriRepository();
            var snapshotStore = CreateSnapshotStore(uriRepository);
            var checkFuzzy = Fuzzy.MatchAll;

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            var contentUris = snapshot.BlobReferences
                .SelectMany(b => b.ContentUris);

            uriRepository.InvalidateValues(contentUris);

            Assert.True(
               snapshotStore.CheckSnapshotExists(
                   snapshot.SnapshotId,
                   checkFuzzy));

            Assert.False(
                snapshotStore.CheckSnapshotValid(
                    snapshot.SnapshotId,
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
        public static void RestoreSnapshot_Writes_Content_To_Streams()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobs = CreateBlobs(new[] { "some content" });

            var snapshot = snapshotStore.AppendSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            using var writeStream = new MemoryStream();

            var restoredBlobs = snapshotStore.RestoreSnapshot(
                snapshot.SnapshotId,
                Fuzzy.MatchAll,
                _ => writeStream);

            Assert.Equal(blobs, restoredBlobs);

            Assert.Equal(
                ToBytes("some content"),
                writeStream.ToArray());
        }

        [Fact]
        public static void RestoreSnapshot_Throws_On_Empty_SnapshotStore()
        {
            var snapshotStore = CreateSnapshotStore();

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.RestoreSnapshot(
                    SnapshotStore.LatestSnapshotId,
                    Fuzzy.MatchAll,
                    _ => new MemoryStream()));
        }

        [Fact]
        public static void ShowSnapshot_Lists_Content()
        {
            var snapshotStore = CreateSnapshotStore();
            var blobs = CreateBlobs(new[] { "some content" });

            var snapshot = snapshotStore.AppendSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            var blobReferences = snapshotStore.ShowSnapshot(
                snapshot.SnapshotId,
                Fuzzy.MatchAll);

            Assert.Equal(
                blobs.Select(b => b.Name),
                blobReferences.Select(c => c.Name));
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

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            snapshotStore.GarbageCollect();

            Assert.True(
                snapshotStore.CheckSnapshotValid(
                    snapshot.SnapshotId,
                    Fuzzy.MatchAll));
        }

        [Fact]
        public static void GarbageCollect_Removes_Unused_Uris()
        {
            var intRepository = CreateIntRepository();
            var snapshotStore = CreateSnapshotStore(
                intRepository: intRepository);

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            intRepository.RemoveValue(snapshot.SnapshotId);

            snapshotStore.GarbageCollect();

            Assert.Empty(intRepository.ListKeys());
        }

        [Fact]
        public static void RemoveSnapshot_Removes_Existing_Snapshots()
        {
            var snapshotStore = CreateSnapshotStore();

            var firstSnapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            snapshotStore.RemoveSnapshot(firstSnapshot.SnapshotId);
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

            snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            var secondSnapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            snapshotStore.KeepSnapshots(1);

            Assert.Equal(
                new[] { secondSnapshot },
                snapshotStore.GetSnapshots());
        }

        [Fact]
        public static void KeepSnapshots_Does_Nothing_If_Equals_Or_Greater_Than_Current()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            snapshotStore.KeepSnapshots(1);
            snapshotStore.KeepSnapshots(2);

            Assert.Equal(
                new[] { snapshot },
                snapshotStore.GetSnapshots());
        }

        [Fact]
        public static void KeepSnapshots_Can_Empty_Store()
        {
            var snapshotStore = CreateSnapshotStore();

            snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            snapshotStore.KeepSnapshots(0);

            Assert.Empty(snapshotStore.GetSnapshots());
        }

        [Fact]
        public static void Copy_Copies_Everything()
        {
            var sourceUriRepository = CreateUriRepository();
            var sourceIntRepository = CreateIntRepository();
            var snapshotStore = CreateSnapshotStore(
                sourceUriRepository,
                sourceIntRepository);

            snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            var destinationUriRepository = CreateUriRepository();
            var destinationIntRepository = CreateIntRepository();

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
            var uriRepository = CreateUriRepository();
            var snapshotStore = CreateSnapshotStore(
                uriRepository,
                CreateIntRepository());

            snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            uriRepository.InvalidateValues(uriRepository.ListKeys());

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.Copy(
                    CreateUriRepository(),
                    CreateIntRepository()));
        }

        [Fact]
        public static void Copy_Throws_On_Invalid_References()
        {
            var intRepository = CreateIntRepository();
            var snapshotStore = CreateSnapshotStore(
                CreateUriRepository(),
                intRepository);

            snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            intRepository.InvalidateValues(intRepository.ListKeys());

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.Copy(
                    CreateUriRepository(),
                    CreateIntRepository()));
        }

        private static Blob[] CreateBlobs(IEnumerable<string> names)
        {
            return names
                .Select(n => new Blob(n, DateTime.UtcNow))
                .ToArray();
        }

        private static Stream OpenRead(string blobName)
        {
            return new MemoryStream(ToBytes(blobName));
        }

        private static byte[] ToBytes(string content)
        {
            return Encoding.UTF8.GetBytes(content);
        }

        private static SnapshotStore CreateSnapshotStore(
            IRepository<Uri>? uriRepository = null,
            IRepository<int>? intRepository = null)
        {
            uriRepository ??= CreateUriRepository();
            intRepository ??= CreateIntRepository();

            var probe = new DummyProbe();

            return new SnapshotStore(
                new ContentStore(
                    uriRepository,
                    new FastCdc(),
                    Id.AlgorithmSHA256,
                    probe),
                intRepository,
                new DummyPrompt(),
                probe);
        }

        private static IRepository<Uri> CreateUriRepository()
        {
            return new MemoryRepository<Uri>();
        }

        private static IRepository<int> CreateIntRepository()
        {
            return new MemoryRepository<int>();
        }
    }
}
