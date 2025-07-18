namespace Chunkyard.Tests.Infrastructure;

internal sealed class DummyChunker : IChunker
{
    public IEnumerable<byte[]> Chunkify(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        return memory.ToArray().Chunk(4);
    }
}
