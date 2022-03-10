namespace Chunkyard.Tests.Core;

public static class ChunkIdTests
{
    [Fact]
    public static void ComputeChunkId_Creates_ChunkId_From_Chunk()
    {
        var chunk = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var expectedId = new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");

        Assert.Equal(
            expectedId,
            ChunkId.ComputeChunkId(chunk));
    }

    [Theory]
    [InlineData("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e", true)]
    [InlineData("sha256://badbadbad", false)]
    public static void ChunkIdValid_Checks_Chunk_Validity_Using_ChunkId(
        string hash,
        bool expectedValidity)
    {
        var chunk = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var chunkId = new Uri(hash);

        Assert.Equal(
            expectedValidity,
            ChunkId.ChunkIdValid(chunkId, chunk));
    }

    [Fact]
    public static void DeconstructChunkId_Can_Split_ChunkId()
    {
        var expectedId = new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");

        var (hashAlgorithmName, hash) = ChunkId.DeconstructChunkId(
            expectedId);

        var actualId = ChunkId.ToChunkId(hashAlgorithmName, hash);

        Assert.Equal(expectedId, actualId);
    }
}
