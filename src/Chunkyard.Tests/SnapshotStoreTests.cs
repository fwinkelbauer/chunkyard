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

            var snapshot = snapshotStore.GetSnapshot(logPosition!.Value);
            var contentReferences = snapshot.ContentReferences.ToArray();

            Assert.Equal(0, logPosition);
            Assert.Single(contentReferences);
            Assert.Equal(
                "some content",
                contentReferences[0].Name);
        }

        [Fact]
        public static void AppendSnapshot_Detects_Previous_Snapshot()
        {
            var repository = new MemoryRepository();
            var contentStore = new SpyContentStore(repository);
            var snapshotStore = new SnapshotStore(repository, contentStore);

            snapshotStore.AppendSnapshot(
                new[] { ("some content", CreateOpenReadContent()) },
                DateTime.Now);

            snapshotStore.AppendSnapshot(
                new[] { ("some content", CreateOpenReadContent()) },
                DateTime.Now);

            Assert.True(
                contentStore.RegisteredContentNames.Contains("some content"));
        }

        [Fact]
        public static void ListUris_Lists_All_Uris_In_Snapshot()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { ("some content", CreateOpenReadContent()) },
                DateTime.Now);

            // "some content" + snapshot #1 = 2 URIs
            Assert.Equal(2, snapshotStore.ListUris(logPosition!.Value).Count());
        }

        [Fact]
        public static void ListUris_Lists_Nothing_If_Snapshot_Is_Invalid()
        {
            var snapshotStore = CreateSnapshotStore(
                new UnstoredRepository());

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { ("some content", CreateOpenReadContent()) },
                DateTime.Now);

            Assert.Empty(snapshotStore.ListUris(logPosition!.Value));
        }

        [Fact]
        public static void GetSnapshot_Accepts_Negative_LogPositions()
        {
            var snapshotStore = CreateSnapshotStore();

            var logPosition = snapshotStore.AppendSnapshot(
                new[] { ("some content", CreateOpenReadContent()) },
                DateTime.Now);

            var expectedSnapshot = snapshotStore.GetSnapshot(
                logPosition!.Value);

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

            snapshotStore.RestoreSnapshot(
                logPosition!.Value,
                "",
                openWrite);

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
                new SpyContentStore(repository));
        }

        private class SpyContentStore : DecoratorContentStore
        {
            public SpyContentStore(IRepository repository)
                : base(
                    new ContentStore(
                        repository,
                        new FastCdc(),
                        HashAlgorithmName.SHA256,
                        new StaticPrompt()))
            {
                RegisteredContentNames = new List<string>();
            }

            public IList<string> RegisteredContentNames { get; }

            public override void RegisterContent(ContentReference contentReference)
            {
                RegisteredContentNames.Add(contentReference.Name);

                base.RegisterContent(contentReference);
            }
        }
    }
}
