using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Xunit;

namespace Chunkyard.Tests
{
    public static class SnapshotStoreTests
    {
        [Fact]
        public static void AppendSnapshot_Creates_Snapshot()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            var snapshot = snapshotStore.GetSnapshot(logPosition);
            var contentReferences = snapshot.ContentReferences;

            Assert.Equal(0, logPosition);
            Assert.Equal(
                new[] { "some content" },
                contentReferences.Select(c => c.Name));
        }

        [Fact]
        public static void AppendSnapshot_Detects_Previous_Snapshot_To_Deduplicate_Encrypted_Content()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition1 = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            var logPosition2 = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            var snapshot1 = snapshotStore.GetSnapshot(logPosition1);
            var snapshot2 = snapshotStore.GetSnapshot(logPosition2);

            Assert.Equal(
                snapshot1.ContentReferences,
                snapshot2.ContentReferences);
        }

        [Fact]
        public static void AppendSnapshot_Creates_Snapshot_Even_If_Empty()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                Array.Empty<string>(),
                OpenStream(),
                DateTime.Now);

            Assert.Equal(0, logPosition);
        }

        [Fact]
        public static void AppendSnapshot_Creates_Snapshot_Without_New_Data()
        {
            var snapshotStore = CreateSnapshotStore();
            var contents = new[] { "some content" };

            snapshotStore.AppendSnapshot(
                contents,
                OpenStream(),
                DateTime.Now);

            var logPosition = snapshotStore.AppendSnapshot(
                contents,
                OpenStream(),
                DateTime.Now);

            Assert.Equal(1, logPosition);
        }

        [Fact]
        public static void GetSnapshot_Accepts_Negative_LogPositions()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            Assert.Equal(
                snapshotStore.GetSnapshot(logPosition),
                snapshotStore.GetSnapshot(-1));
        }

        [Fact]
        public static void CheckSnapshot_Detects_Ok()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            Assert.True(snapshotStore.CheckSnapshotExists(logPosition));
            Assert.True(snapshotStore.CheckSnapshotValid(logPosition));
        }

        [Fact]
        public static void CheckSnapshot_Throws_If_Snapshot_Missing()
        {
            var snapshotStore = CreateSnapshotStore(
                new UnstoredRepository());

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotExists(logPosition));

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotValid(logPosition));
        }

        [Fact]
        public static void CheckSnapshot_Throws_If_Snapshot_Invalid()
        {
            var snapshotStore = CreateSnapshotStore(
                new CorruptedRepository());

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotExists(logPosition));

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CheckSnapshotValid(logPosition));
        }

        [Fact]
        public static void Restore_Writes_Content_To_Streams()
        {
            var snapshotStore = CreateSnapshotStore();
            var content = new byte[] { 0x11, 0x22, 0x33, 0x44 };
            var bytesPerContent = new Dictionary<string, byte[]>
            {
                { "some content", content }
            };

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(bytesPerContent),
                DateTime.Now);

            var actualContentName = "";
            using var writeStream = new MemoryStream();

            Stream openWrite(string s)
            {
                actualContentName = s;
                return writeStream;
            }

            snapshotStore.RestoreSnapshot(logPosition, "", openWrite);

            Assert.Equal("some content", actualContentName);
            Assert.Equal(content, writeStream.ToArray());
        }

        [Fact]
        public static void ShowSnapshot_Lists_Content()
        {
            var snapshotStore = CreateSnapshotStore();
            var contents = new[] { "some content" };

            var logPosition = snapshotStore.AppendSnapshot(
                contents,
                OpenStream(),
                DateTime.Now);

            Assert.Equal(
                contents,
                snapshotStore.ShowSnapshot(logPosition).Select(c => c.Name));
        }

        [Fact]
        public static void GarbageCollect_Keeps_Used_uris()
        {
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(
                repository);

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            snapshotStore.GarbageCollect();

            Assert.True(snapshotStore.CheckSnapshotValid(logPosition));
        }

        [Fact]
        public static void GarbageCollect_Removes_Unused_Uris()
        {
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(
                repository);

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            repository.RemoveFromLog(logPosition);

            snapshotStore.GarbageCollect();

            Assert.Empty(repository.ListUris());
        }

        [Fact]
        public static void CopySnapshots_Throws_If_RepositoryIds_Dont_Match()
        {
            var snapshotStore = CreateSnapshotStore();
            var otherRepository = new MemoryRepository();

            Assert.Throws<ChunkyardException>(
                () => snapshotStore.CopySnapshots(otherRepository));
        }

        [Fact]
        public static void CopySnapshots_Throws_If_Snapshots_Dont_Match()
        {
            var repository1 = new MemoryRepository();
            var snapshotStore1 = CreateSnapshotStore(repository1);
            var repository2 = new MemoryRepository(repository1.RepositoryId);
            var snapshotStore2 = CreateSnapshotStore(repository2);

            snapshotStore1.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            snapshotStore2.AppendSnapshot(
                new[] { "some other content" },
                OpenStream(),
                DateTime.Now);

            Assert.Throws<ChunkyardException>(
                () => snapshotStore1.CopySnapshots(repository2));
        }

        [Fact]
        public static void CopySnapshots_Copies_Missing_Data()
        {
            var repository = new MemoryRepository();
            var snapshotStore = CreateSnapshotStore(repository);
            var otherRepository = new MemoryRepository(
                repository.RepositoryId);

            snapshotStore.AppendSnapshot(
                new[] { "some content" },
                OpenStream(),
                DateTime.Now);

            snapshotStore.CopySnapshots(otherRepository);

            Assert.Equal(
                repository.ListUris(),
                otherRepository.ListUris());
        }

        private static Func<string, Stream> OpenStream(
            Dictionary<string, byte[]>? bytesPerContent = null)
        {
            return (s) => new MemoryStream(
                bytesPerContent == null
                    ? new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }
                    : bytesPerContent[s]);
        }

        private static SnapshotStore CreateSnapshotStore(
            IRepository? repository = null)
        {
            return new SnapshotStore(
                new ContentStore(
                    repository ?? new MemoryRepository(),
                    new FastCdc(),
                    HashAlgorithmName.SHA256,
                    new StaticPrompt()));
        }
    }
}
