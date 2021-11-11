namespace Chunkyard.Tests.Core
{
    public static class DiffSetTests
    {
        [Fact]
        public static void DiffSet_Outlines_Differences_Between_Collections()
        {
            var date = DateTime.UtcNow;
            var nonce = new byte[] { 0xFF };

            var first = new[]
            {
                new BlobReference(
                    "some blob",
                    date,
                    nonce,
                    new[] { new Uri("some://uri") }),
                new BlobReference(
                    "changed uri blob",
                    date,
                    nonce,
                    new[] { new Uri("some://uri") }),
                new BlobReference(
                    "changed date blob",
                    date,
                    nonce,
                    new[] { new Uri("some://uri") }),
                new BlobReference(
                    "removed blob",
                    date,
                    nonce,
                    new[] { new Uri("some://uri") })
            };

            var second = new[]
            {
                new BlobReference(
                    "some blob",
                    date,
                    nonce,
                    new[] { new Uri("some://uri") }),
                new BlobReference(
                    "changed uri blob",
                    date,
                    nonce,
                    new[] { new Uri("some://new.uri") }),
                new BlobReference(
                    "changed date blob",
                    date.AddSeconds(1),
                    nonce,
                    new[] { new Uri("some://uri") }),
                new BlobReference(
                    "new blob",
                    date,
                    nonce,
                    new[] { new Uri("some://uri") })
            };

            var expectedDiff = new DiffSet(
                new[] { "new blob" },
                new[] { "changed uri blob", "changed date blob" },
                new[] { "removed blob" });

            Assert.Equal(
                expectedDiff,
                DiffSet.Create(
                    first,
                    second,
                    br => br.Name));
        }
    }
}
