namespace Chunkyard.Tests.Core;

public static class FastCdcTests
{
    private static readonly byte[] _pictureBytes = File.ReadAllBytes(
        "Assets/SekienAkashita.jpg");

    [Fact]
    public static void SplitIntoChunks_16k_Chunks()
    {
        var fastCdc = new FastCdc(
            8 * 1024,
            16 * 1024,
            32 * 1024);

        using var stream = new MemoryStream(_pictureBytes);
        var chunks = fastCdc.SplitIntoChunks(stream).ToArray();

        Assert.Equal(
            new[] { 22366, 8282, 16303, 18696, 32768, 11051 },
            chunks.Select(c => c.Length));

        Assert.Equal(
            _pictureBytes,
            chunks.SelectMany(c => c));
    }

    [Fact]
    public static void SplitIntoChunks_32k_Chunks()
    {
        var fastCdc = new FastCdc(
            16 * 1024,
            32 * 1024,
            64 * 1024);

        using var stream = new MemoryStream(_pictureBytes);
        var chunks = fastCdc.SplitIntoChunks(stream).ToArray();

        Assert.Equal(
            new[] { 32857, 16408, 60201 },
            chunks.Select(c => c.Length));

        Assert.Equal(
            _pictureBytes,
            chunks.SelectMany(c => c));
    }

    [Fact]
    public static void SplitIntoChunks_64k_Chunks()
    {
        var fastCdc = new FastCdc(
            32 * 1024,
            64 * 1024,
            128 * 1024);

        using var stream = new MemoryStream(_pictureBytes);
        var chunks = fastCdc.SplitIntoChunks(stream).ToArray();

        Assert.Equal(
            new[] { 32857, 76609 },
            chunks.Select(c => c.Length));

        Assert.Equal(
            _pictureBytes,
            chunks.SelectMany(c => c));
    }
}
