namespace Chunkyard.Tests.Core;

[TestClass]
public sealed class FastCdcTests
{
    private static readonly byte[] PictureBytes = File.ReadAllBytes(
        "Assets/SekienAkashita.jpg");

    [TestMethod]
    [DataRow(8, new[] { 22366, 8282, 16303, 18696, 32768, 11051 })]
    [DataRow(16, new[] { 32857, 16408, 60201 })]
    [DataRow(32, new[] { 32857, 76609 })]
    public void Chunkify_Splits_Data_Into_Ordered_List_Of_Pieces(int factor, int[] chunkSizes)
    {
        var minSize = factor * 1024;
        var avgSize = factor * 2 * 1024;
        var maxSize = factor * 4 * 1024;
        var chunker = new FastCdc(minSize, avgSize, maxSize);

        using var stream = new MemoryStream(PictureBytes);
        var chunks = chunker.Chunkify(stream).ToArray();

        CollectionAssert.AreEqual(
            chunkSizes,
            chunks.Select(c => c.Length).ToArray());

        CollectionAssert.AreEqual(
            PictureBytes,
            chunks.SelectMany(c => c).ToArray());
    }
}
