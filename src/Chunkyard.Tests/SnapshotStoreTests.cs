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
                new[] { ("some content", CreateOpenReadContent()) },
                DateTime.Now);

            var snapshot = snapshotStore.GetSnapshot(logPosition);
            var contentReferences = snapshot.ContentReferences.ToArray();

            Assert.Equal(0, logPosition);
            Assert.Single(contentReferences);
            Assert.Equal(
                "some content",
                contentReferences[0].Name);
        }

        [Fact]
        public static void AppendSnapshot_Detects_Previous_Snapshot_To_Deduplicate_Encrypted_Content()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition1 = snapshotStore.AppendSnapshot(
                new[] { ("some content", CreateOpenReadContent()) },
                DateTime.Now);

            var logPosition2 = snapshotStore.AppendSnapshot(
                new[] { ("some content", CreateOpenReadContent()) },
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
                Array.Empty<(string, Func<Stream>)>(),
                DateTime.Now);

            Assert.Equal(0, logPosition);
        }

        [Fact]
        public static void AppendSnapshot_Creates_Snapshot_Without_New_Data()
        {
            var snapshotStore = CreateSnapshotStore();
            var contents = new[] { ("some content", CreateOpenReadContent()) };

            snapshotStore.AppendSnapshot(
                contents,
                DateTime.Now);

            var logPosition = snapshotStore.AppendSnapshot(
                contents,
                DateTime.Now);

            Assert.Equal(1, logPosition);
        }

        [Fact]
        public static void ListUris_Lists_All_Uris_In_Snapshot()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { ("some content", CreateOpenReadContent()) },
                DateTime.Now);

            // - A URI for "some content"
            // - A URI for the snapshot itself
            Assert.Equal(2, snapshotStore.ListUris(logPosition).Count());
        }

        [Fact]
        public static void ListUris_Throws_If_Snapshot_Is_Invalid()
        {
            var snapshotStore = CreateSnapshotStore(
                new UnstoredRepository());

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { ("some content", CreateOpenReadContent()) },
                DateTime.Now);

            // xunit does not let my catch a generic Exception type. Instead I
            // have to check for the KeyNotFoundException which only happens in
            // a in-memory repository
            Assert.Throws<KeyNotFoundException>(
                () => snapshotStore.ListUris(logPosition));
        }

        [Fact]
        public static void GetSnapshot_Accepts_Negative_LogPositions()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { ("some content", CreateOpenReadContent()) },
                DateTime.Now);

            var expectedSnapshot = snapshotStore.GetSnapshot(logPosition);
            var actualSnapshot = snapshotStore.GetSnapshot(-1);

            Assert.Equal(expectedSnapshot, actualSnapshot);
        }

        [Fact]
        public static void Can_Create_And_Restore_Snapshot()
        {
            var snapshotStore = CreateSnapshotStore();
            var content = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { ("some content", CreateOpenReadContent(content)) },
                DateTime.Now);

            var actualContentName = "";
            using var writeStream = new MemoryStream();
            Func<string, Stream> openWrite = (s) =>
            {
                actualContentName = s;
                return writeStream;
            };

            snapshotStore.RestoreSnapshot(logPosition, "", openWrite);

            Assert.Equal("some content", actualContentName);
            Assert.Equal(content, writeStream.ToArray());
        }

        private static Func<Stream> CreateOpenReadContent(
            byte[]? content = null)
        {
            return () => new MemoryStream(
                content ?? new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
        }

        private static SnapshotStore CreateSnapshotStore(
            IRepository? repository = null)
        {
            repository = repository ?? new MemoryRepository();

            return new SnapshotStore(
                repository,
                new ContentStore(
                    repository,
                    new FastCdc(),
                    HashAlgorithmName.SHA256,
                    new StaticPrompt()));
        }
    }
}
