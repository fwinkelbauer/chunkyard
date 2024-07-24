namespace Chunkyard.Tests.Core;

public static class FastCdcTests
{
    private static readonly byte[] PictureBytes = File.ReadAllBytes(
        "Assets/SekienAkashita.jpg");

    [Theory]
    [InlineData(8, new[] { 22366, 8282, 16303, 18696, 32768, 11051 })]
    [InlineData(16, new[] { 32857, 16408, 60201 })]
    [InlineData(32, new[] { 32857, 76609 })]
    public static void SplitIntoChunks_Splits_Data_Into_Ordered_List_Of_Pieces(int factor, int[] chunkSizes)
    {
        var minSize = factor * 1024;
        var avgSize = factor * 2 * 1024;
        var maxSize = factor * 4 * 1024;
        var fastCdc = new FastCdc(minSize, avgSize, maxSize);

        using var stream = new MemoryStream(PictureBytes);
        var chunks = fastCdc.SplitIntoChunks(stream).ToArray();

        Assert.Equal(
            chunkSizes,
            chunks.Select(c => c.Length));

        Assert.Equal(
            PictureBytes,
            chunks.SelectMany(c => c));
    }
}
