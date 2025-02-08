namespace Chunkyard.Tests.Infrastructure;

internal sealed class DummyChunker : IChunker
{
    public IEnumerable<byte[]> Chunkify(Stream stream)
    {
        var buffer = new byte[1];
        var bytesRead = 0;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            yield return buffer.ToArray();
        }
    }
}
