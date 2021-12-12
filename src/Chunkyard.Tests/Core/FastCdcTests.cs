namespace Chunkyard.Tests.Core;

public static class FastCdcTests
{
    private static readonly byte[] _pictureBytes = File.ReadAllBytes(
        "Assets/SekienAkashita.jpg");

    [Fact]
    public static void SplitIntoChunks_16k_Chunks()
    {
        AssertSplit(
            new FastCdc(
                8 * 1024,
                16 * 1024,
                32 * 1024),
            new[] { 22366, 8282, 16303, 18696, 32768, 11051 });
    }

    [Fact]
    public static void SplitIntoChunks_32k_Chunks()
    {
        AssertSplit(
            new FastCdc(
                16 * 1024,
                32 * 1024,
                64 * 1024),
            new[] { 32857, 16408, 60201 });
    }

    [Fact]
    public static void SplitIntoChunks_64k_Chunks()
    {
        AssertSplit(
            new FastCdc(
                32 * 1024,
                64 * 1024,
                128 * 1024),
            new[] { 32857, 76609 });
    }

    private static void AssertSplit(FastCdc fastCdc, int[] chunkSizes)
    {
        using var stream = new MemoryStream(_pictureBytes);
        var chunks = fastCdc.SplitIntoChunks(stream).ToArray();

        Assert.Equal(
            chunkSizes,
            chunks.Select(c => c.Length));

        Assert.Equal(
            _pictureBytes,
            chunks.SelectMany(c => c));
    }
}
