namespace Chunkyard.Tests.Core;

public static class FastCdcTests
{
    private static readonly byte[] _pictureBytes = File.ReadAllBytes(
        "Assets/SekienAkashita.jpg");

    public static TheoryData<FastCdc, int[]> TheoryData => new()
    {
        {
            new FastCdc(8 * 1024, 16 * 1024, 32 * 1024),
            new[] { 22366, 8282, 16303, 18696, 32768, 11051 }
        },
        {
            new FastCdc(16 * 1024, 32 * 1024, 64 * 1024),
            new[] { 32857, 16408, 60201 }
        },
        {
            new FastCdc(32 * 1024, 64 * 1024, 128 * 1024),
            new[] { 32857, 76609 }
        }
    };

    [Theory, MemberData(nameof(TheoryData))]
    public static void SplitIntoChunks(FastCdc fastCdc, int[] chunkSizes)
    {
        ArgumentNullException.ThrowIfNull(fastCdc);

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
