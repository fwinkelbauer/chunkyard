namespace Chunkyard.Tests.Core;

public static class ChunkIdTests
{
    [Fact]
    public static void Compute_Creates_ChunkId_From_Chunk()
    {
        var chunk = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var expectedId = "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e";

        Assert.Equal(expectedId, ChunkId.Compute(chunk));
    }

    [Theory]
    [InlineData("ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e", true)]
    [InlineData("badbadbad", false)]
    public static void Valid_Checks_Chunk_Validity_Using_ChunkId(
        string chunkId,
        bool expectedValidity)
    {
        var chunk = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        Assert.Equal(expectedValidity, ChunkId.Valid(chunkId, chunk));
    }

    [Fact]
    public static void Shorten_Creates_Shorter_ChunkId()
    {
        var chunkId = "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e";

        Assert.Equal("ad95131bc0b7", ChunkId.Shorten(chunkId));
    }
}
