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
                DateTime.Now,
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
                DateTime.Now,
                OpenRead);

            var snapshot2 = snapshotStore.AppendSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.Now,
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
                DateTime.Now,
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
                DateTime.Now,
                OpenRead);

            var snapshot = snapshotStore.AppendSnapshot(
                blobs,
                Fuzzy.MatchNothing,
                DateTime.Now,
                OpenRead);

            Assert.Equal(1, snapshot.SnapshotId);
        }

        [Fact]
        public static void New_SnapshotStore_Can_Read_Existing_Snapshot()
        {
            var repository = new MemoryRepository();

            var expectedNames = new[] { "some content" };

            var snapshotId = CreateSnapshotStore(repository).AppendSnapshot(
                CreateBlobs(expectedNames),
                Fuzzy.MatchNothing,
                DateTime.Now,
                OpenRead).SnapshotId;

            var actualNames = CreateSnapshotStore(repository)
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
                DateTime.Now,
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
                DateTime.Now,
                OpenRead);

            Assert.True(
                snapshotStore.CheckSnapshotExists(snapshot.SnapshotId, fuzzy));

            Assert.True(
                snapshotStore.CheckSnapshotValid(snapshot.SnapshotId, fuzzy));
        }

        [Fact]
        public static void CheckSnapshot_Throws_If_Snapshot_Missing()
        {
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(repository);
            var fuzzy = Fuzzy.MatchAll;

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.Now,
                OpenRead);

            RemoveValues(repository, repository.ListUris());

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
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(repository);
            var fuzzy = Fuzzy.MatchAll;

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.Now,
                OpenRead);

            CorruptValues(repository, repository.ListUris());

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
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(repository);
            var fuzzy = Fuzzy.MatchAll;

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.Now,
                OpenRead);

            var contentUris = snapshot.BlobReferences
                .SelectMany(blobReference => blobReference.ContentUris);

            RemoveValues(repository, contentUris);

            Assert.False(
                snapshotStore.CheckSnapshotExists(snapshot.SnapshotId, fuzzy));

            Assert.False(
                snapshotStore.CheckSnapshotValid(snapshot.SnapshotId, fuzzy));
        }

        [Fact]
        public static void CheckSnapshot_Detects_Invalid_Content()
        {
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(repository);
            var fuzzy = Fuzzy.MatchAll;

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.Now,
                OpenRead);

            var contentUris = snapshot.BlobReferences
                .SelectMany(blobReference => blobReference.ContentUris);

            CorruptValues(repository, contentUris);

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
                DateTime.Now,
                OpenRead);

            var actualBlobName = "";
            using var writeStream = new MemoryStream();

            Stream OpenWrite(string blobName)
            {
                actualBlobName = blobName;
                return writeStream;
            }

            snapshotStore.RestoreSnapshot(
                snapshot.SnapshotId,
                Fuzzy.MatchAll,
                OpenWrite);

            Assert.Equal("some content", actualBlobName);
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
                DateTime.Now,
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
                DateTime.Now,
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
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(repository);

            var snapshot = snapshotStore.AppendSnapshot(
                CreateBlobs(new[] { "some content" }),
                Fuzzy.MatchNothing,
                DateTime.Now,
                OpenRead);

            repository.RemoveFromLog(snapshot.SnapshotId);

            snapshotStore.GarbageCollect();

            Assert.Empty(repository.ListUris());
        }

        private static Blob[] CreateBlobs(IEnumerable<string> names)
        {
            return names
                .Select(name => new Blob(name, DateTime.Now, DateTime.Now))
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
            IRepository? repository = null)
        {
            repository ??= new MemoryRepository();

            return new SnapshotStore(
                new ContentStore(
                    repository,
                    new FastCdc(),
                    HashAlgorithmName.SHA256),
                repository,
                new StaticPrompt());
        }

        private static void CorruptValues(
            IRepository repository,
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
            IRepository repository,
            IEnumerable<Uri> contentUris)
        {
            foreach (var contentUri in contentUris)
            {
                repository.RemoveValue(contentUri);
            }
        }
    }
}
