namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IChunker"/> without any chunking.
/// </summary>
public sealed class SimpleChunker : IChunker
{
    public IEnumerable<byte[]> Chunkify(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        yield return memory.ToArray();
    }
}
