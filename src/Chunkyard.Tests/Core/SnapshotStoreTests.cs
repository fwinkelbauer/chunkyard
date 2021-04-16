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
        public static void AppendSnapshot_Creates_Snapshot()
        {
            var snapshotStore = CreateSnapshotStore();

            var expectedNames = new[] { "some content" };

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(expectedNames),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            var blobReferences = snapshot.BlobReferences;

            Assert.Equal(0, snapshot.SnapshotId);

            Assert.Equal(
                snapshot,
                snapshotStore.GetSnapshot(snapshot.SnapshotId));

            Assert.Equal(
                new[] { snapshot },
                snapshotStore.GetSnapshots());

            Assert.Equal(
                expectedNames,
                blobReferences.Select(c => c.Name));
        }

        [Fact]
        public static void AppendSnapshot_Detects_Previous_Snapshot_To_Deduplicate_Encrypted_Content()
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
                OpenRead);

            Assert.Equal(
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
                    OpenRead).SnapshotId;

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
                snapshotStore.GetSnapshot(-1));
        }

        [Fact]
        public static void CheckSnapshot_Detects_Ok()
        {
            var snapshotStore = CreateSnapshotStore();
            var fuzzy = Fuzzy.MatchAll;

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            Assert.True(
                snapshotStore.CheckSnapshotExists(snapshot.SnapshotId, fuzzy));

            Assert.True(
                snapshotStore.CheckSnapshotValid(snapshot.SnapshotId, fuzzy));
        }

        [Fact]
        public static void CheckSnapshot_Throws_If_Snapshot_Missing()
        {
            var uriRepository = CreateUriRepository();
            var snapshotStore = CreateSnapshotStore(uriRepository);
            var fuzzy = Fuzzy.MatchAll;

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            RemoveValues(uriRepository, uriRepository.ListKeys());

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotExists(
                    snapshot.SnapshotId,
                    fuzzy));

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotValid(
                    snapshot.SnapshotId,
                    fuzzy));
        }

        [Fact]
        public static void CheckSnapshot_Throws_If_Snapshot_Invalid()
        {
            var uriRepository = CreateUriRepository();
            var snapshotStore = CreateSnapshotStore(uriRepository);
            var fuzzy = Fuzzy.MatchAll;

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            CorruptValues(uriRepository, uriRepository.ListKeys());

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotExists(
                    snapshot.SnapshotId,
                    fuzzy));

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotValid(
                    snapshot.SnapshotId,
                    fuzzy));
        }

        [Fact]
        public static void CheckSnapshot_Detects_Missing_Content()
        {
            var uriRepository = CreateUriRepository();
            var snapshotStore = CreateSnapshotStore(uriRepository);
            var fuzzy = Fuzzy.MatchAll;

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            var contentUris = snapshot.BlobReferences
                .SelectMany(b => b.ContentUris);

            RemoveValues(uriRepository, contentUris);

            Assert.False(
                snapshotStore.CheckSnapshotExists(snapshot.SnapshotId, fuzzy));

            Assert.False(
                snapshotStore.CheckSnapshotValid(snapshot.SnapshotId, fuzzy));
        }

        [Fact]
        public static void CheckSnapshot_Detects_Invalid_Content()
        {
            var uriRepository = CreateUriRepository();
            var snapshotStore = CreateSnapshotStore(uriRepository);
            var fuzzy = Fuzzy.MatchAll;

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            var contentUris = snapshot.BlobReferences
                .SelectMany(b => b.ContentUris);

            CorruptValues(uriRepository, contentUris);

            Assert.True(
               snapshotStore.CheckSnapshotExists(snapshot.SnapshotId, fuzzy));

            Assert.False(
                snapshotStore.CheckSnapshotValid(snapshot.SnapshotId, fuzzy));
        }

        [Fact]
        public static void Restore_Writes_Content_To_Streams()
        {
            var snapshotStore = CreateSnapshotStore();

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.UtcNow,
                OpenRead);

            using var writeStream = new MemoryStream();

            Stream OpenWrite(string blobName)
            {
                return writeStream;
            }

            var restoredBlobs = snapshotStore.RestoreSnapshot(
                snapshot.SnapshotId,
                Fuzzy.MatchAll,
                OpenWrite);

            Assert.Equal(
                new[] { "some content" },
                restoredBlobs.Select(b => b.Name));

            Assert.Equal(
                ToBytes("some content"),
                writeStream.ToArray());
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

        private static Blob[] CreateBlobs(IEnumerable<string> names)
        {
            return names
                .Select(n => new Blob(n, DateTime.UtcNow, DateTime.UtcNow))
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

            return new SnapshotStore(
                new ContentStore(
                    uriRepository,
                    new FastCdc(),
                    HashAlgorithmName.SHA256),
                intRepository,
                new StaticPrompt());
        }

        private static IRepository<Uri> CreateUriRepository()
        {
            return new MemoryRepository<Uri>();
        }

        private static IRepository<int> CreateIntRepository()
        {
            return new MemoryRepository<int>();
        }

        private static void CorruptValues(
            IRepository<Uri> repository,
            IEnumerable<Uri> contentUris)
        {
            foreach (var contentUri in contentUris)
            {
                repository.RemoveValue(contentUri);
                repository.StoreValue(
                    contentUri,
                    new byte[] { 0xFF, 0xBA, 0xDD, 0xFF });
            }
        }

        private static void RemoveValues(
            IRepository<Uri> repository,
            IEnumerable<Uri> contentUris)
        {
            foreach (var contentUri in contentUris)
            {
                repository.RemoveValue(contentUri);
            }
        }
    }
}
