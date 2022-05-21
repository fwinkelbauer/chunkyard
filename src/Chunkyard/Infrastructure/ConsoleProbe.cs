namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IProbe"/> which writes to the console.
/// </summary>
internal class ConsoleProbe : IProbe
{
    public void StoredBlob(string blobName)
    {
        Console.WriteLine($"Stored blob: {blobName}");
    }

    public void RestoredBlob(string blobName)
    {
        Console.WriteLine($"Restored blob: {blobName}");
    }

    public void RemovedBlob(string blobName)
    {
        Console.WriteLine($"Removed blob: {blobName}");
    }

    public void BlobValid(string blobName, bool valid)
    {
        Console.WriteLine(valid
            ? $"Valid blob: {blobName}"
            : $"Invalid blob: {blobName}");
    }

    public void CopiedChunk(string chunkId)
    {
        Console.WriteLine($"Copied chunk: {ChunkId.Shorten(chunkId)}");
    }

    public void RemovedChunk(string chunkId)
    {
        Console.WriteLine($"Removed chunk: {ChunkId.Shorten(chunkId)}");
    }

    public void CopiedSnapshot(int snapshotId)
    {
        Console.WriteLine($"Copied snapshot: #{snapshotId}");
    }

    public void StoredSnapshot(int snapshotId)
    {
        Console.WriteLine($"Stored snapshot: #{snapshotId}");
    }

    public void RestoredSnapshot(int snapshotId)
    {
        Console.WriteLine($"Restored snapshot: #{snapshotId}");
    }

    public void RemovedSnapshot(int snapshotId)
    {
        Console.WriteLine($"Removed snapshot: #{snapshotId}");
    }

    public void SnapshotValid(int snapshotId, bool valid)
    {
        Console.WriteLine(valid
            ? $"Valid snapshot: #{snapshotId}"
            : $"Invalid snapshot: #{snapshotId}");
    }
}
