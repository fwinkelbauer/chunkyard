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

            var content = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            using var memoryStream = new MemoryStream(content);

            snapshotBuilder.AddContent(memoryStream, "some content");
            var logPosition = snapshotBuilder.WriteSnapshot(DateTime.Now);
            var snapshot = snapshotBuilder.GetSnapshot(logPosition);

            Assert.Equal(1, logPosition);
            Assert.Single(snapshot.ContentReferences);
            Assert.Equal(
                2,
                snapshotBuilder.ListUris(logPosition).Count());
        }

        private static SnapshotBuilder CreateSnapshotBuilder()
        {
            return new SnapshotBuilder(
                new ContentStore(
                    new MemoryRepository(),
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
