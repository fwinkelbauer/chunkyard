namespace Chunkyard.Tests.Core;

[TestClass]
public sealed class DiffSetTests
{
    [TestMethod]
    public void DiffSet_Outlines_Differences_Between_Collections()
    {
        var date = DateTime.UtcNow;
        var unchangedBlob = new Blob("some blob", date);
        var changedBlob = new Blob("changed date blob", date.AddSeconds(1));
        var removedBlob = new Blob("removed blob", date);

        var first = new[]
        {
            unchangedBlob,
            changedBlob,
            removedBlob
        };

        changedBlob = new Blob(
            changedBlob.Name,
            changedBlob.LastWriteTimeUtc.AddSeconds(1));

        var newBlob = new Blob("new blob", date);

        var second = new[]
        {
            unchangedBlob,
            changedBlob,
            newBlob
        };

        var expected = new DiffSet<Blob>(
            new[] { newBlob },
            new[] { changedBlob },
            new[] { removedBlob });

        var actual = DiffSet.Create(
            first,
            second,
            br => br.Name);

        Assert.AreEqual(expected, actual);
    }
}
