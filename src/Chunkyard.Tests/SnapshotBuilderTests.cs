using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Xunit;

namespace Chunkyard.Tests
{
    public static class SnapshotBuilderTests
    {
        [Fact]
        public static void WriteSnapshot_Creates_Snapshot()
        {
            var snapshotBuilder = CreateSnapshotBuilder();

            using var contentStream = CreateContent();

            snapshotBuilder.AddContent(contentStream, "some content");
            var logPosition = snapshotBuilder.WriteSnapshot(DateTime.Now);
            var snapshot = snapshotBuilder.GetSnapshot(logPosition);
            var contentReferences = snapshot.ContentReferences.ToArray();

            Assert.Equal(0, logPosition);
            Assert.Single(contentReferences);
            Assert.Equal(
                "some content",
                contentReferences[0].Name);
        }

        [Fact]
        public static void AddContent_Detects_Previous_Content()
        {
            var repository = new MemoryRepository();
            var snapshotBuilder = CreateSnapshotBuilder(repository);

            using var contentStream1 = CreateContent();

            snapshotBuilder.AddContent(contentStream1, "some content");
            snapshotBuilder.WriteSnapshot(DateTime.Now);

            using var contentStream2 = CreateContent();
            snapshotBuilder.AddContent(contentStream2, "some content");
            snapshotBuilder.WriteSnapshot(DateTime.Now);

            // "some content" + snapshot #1 + snapshot #2 = 3 URIs
            Assert.Equal(3, repository.ListUris().Count());
        }

        [Fact]
        public static void ListUris_Lists_All_Uris_In_Snapshot()
        {
            var snapshotBuilder = CreateSnapshotBuilder();

            using var contentStream = CreateContent();

            snapshotBuilder.AddContent(contentStream, "some content");
            var logPosition = snapshotBuilder.WriteSnapshot(DateTime.Now);

            // "some content" + snapshot #1 = 2 URIs
            Assert.Equal(2, snapshotBuilder.ListUris(logPosition).Count());
        }

        [Fact]
        public static void ListUris_Lists_Nothing_If_Snapshot_Is_Invalid()
        {
            var repository = new MemoryRepository();
            var snapshotBuilder = CreateSnapshotBuilder(repository);

            using var contentStream = CreateContent();

            snapshotBuilder.AddContent(contentStream, "some content");
            var logPosition = snapshotBuilder.WriteSnapshot(DateTime.Now);
            var uris = snapshotBuilder.ListUris(logPosition).ToArray();

            repository.RemoveValue(uris[0]);

            Assert.Empty(snapshotBuilder.ListUris(logPosition));
        }

        [Fact]
        public static void GetSnapshot_Accepts_Negative_LogPositions()
        {
            var snapshotBuilder = CreateSnapshotBuilder();

            var logPosition = snapshotBuilder.WriteSnapshot(DateTime.Now);
            var expectedSnapshot = snapshotBuilder.GetSnapshot(logPosition);
            var actualSnapshot = snapshotBuilder.GetSnapshot(-1);

            Assert.Equal(expectedSnapshot, actualSnapshot);
        }

        private static Stream CreateContent()
        {
            return new MemoryStream(
                new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
        }

        private static SnapshotBuilder CreateSnapshotBuilder(
            IRepository? repository = null)
        {
            return new SnapshotBuilder(
                new ContentStore(
                    repository ?? new MemoryRepository(),
                    new FastCdc(
                        2 * 1024 * 1024,
                        4 * 1024 * 1024,
                        8 * 1024 * 1024),
                    HashAlgorithmName.SHA256,
                    "secret password",
                    AesGcmCrypto.GenerateSalt(),
                    5),
                null);
        }
    }
}
