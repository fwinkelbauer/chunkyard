namespace Chunkyard.Tests;

public static class StreamUtils
{
    public static byte[] AsBytes(Func<Stream> streamFunc)
    {
        using var memoryStream = new MemoryStream();
        using var stream = streamFunc();

        stream.CopyTo(memoryStream);

        return memoryStream.ToArray();
    }
}
