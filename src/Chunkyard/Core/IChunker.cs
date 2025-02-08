namespace Chunkyard.Core;

/// <summary>
/// Defines a way to split streams into chunks/pieces.
/// </summary>
public interface IChunker
{
    IEnumerable<byte[]> Chunkify(Stream stream);
}
