namespace Chunkyard.Tests.Core;

[TestClass]
public sealed class ChunkIdTests
{
    [TestMethod]
    public void Compute_Creates_ChunkId_From_Chunk()
    {
        var chunk = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        Assert.AreEqual(
            "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e",
            ChunkId.Compute(chunk));
    }

    [TestMethod]
    [DataRow("ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e", true)]
    [DataRow("badbadbad", false)]
    public void Valid_Checks_Chunk_Validity_Using_ChunkId(
        string chunkId,
        bool expected)
    {
        var chunk = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        Assert.AreEqual(expected, ChunkId.Valid(chunkId, chunk));
    }
}
