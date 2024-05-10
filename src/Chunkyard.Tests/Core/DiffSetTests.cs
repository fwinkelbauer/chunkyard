namespace Chunkyard.Tests.Core;

public static class DiffSetTests
{
    [Fact]
    public static void DiffSet_Outlines_Differences_Between_Collections()
    {
        var date = DateTime.UtcNow;

        var first = new[]
        {
            new Blob("some blob", date),
            new Blob("changed date blob", date),
            new Blob("removed blob", date)
        };

        var second = new[]
        {
            new Blob("some blob", date),
            new Blob("changed date blob", date.AddSeconds(1)),
            new Blob("new blob", date)
        };

        var expectedDiff = new DiffSet(
            new[] { "new blob" },
            new[] { "changed date blob" },
            new[] { "removed blob" });

        Assert.Equal(
            expectedDiff,
            DiffSet.Create(
                first,
                second,
                br => br.Name));
    }
}
