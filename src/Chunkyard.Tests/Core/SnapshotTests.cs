using System;
using Chunkyard.Core;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class SnapshotTests
    {
        [Fact]
        public static void Diff_Shows_Differences_Between_Snapshots()
        {
            var date = DateTime.UtcNow;
            var nonce = new byte[] { 0xFF };

            var snapshot1 = new Snapshot(
                1,
                date,
                new[]
                {
                    new BlobReference(
                        "some blob",
                        date,
                        nonce,
                        new[] { new Uri("some://uri") }),
                    new BlobReference(
                        "changed blob",
                        date,
                        nonce,
                        new[] { new Uri("some://uri") }),
                    new BlobReference(
                        "removed blob",
                        date,
                        nonce,
                        new[] { new Uri("some://uri") })
                });

            var snapshot2 = new Snapshot(
                2,
                date,
                new[]
                {
                    new BlobReference(
                        "some blob",
                        date,
                        nonce,
                        new[] { new Uri("some://uri") }),
                    new BlobReference(
                        "changed blob",
                        date,
                        nonce,
                        new[] { new Uri("some://new.uri") }),
                    new BlobReference(
                        "new blob",
                        date,
                        nonce,
                        new[] { new Uri("some://uri") })
                });

            var expectedDiff = new DiffSet(
                new[] { "new blob" },
                new[] { "changed blob" },
                new[] { "removed blob" });

            Assert.Equal(
                expectedDiff,
                Snapshot.Diff(snapshot1, snapshot2));
        }
    }
}
