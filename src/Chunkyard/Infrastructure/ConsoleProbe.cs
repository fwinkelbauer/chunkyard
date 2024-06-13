namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IProbe"/> which writes to the console.
/// </summary>
internal sealed class ConsoleProbe : IProbe
{
    public void StoredBlob(Blob blob)
    {
        Console.Error.WriteLine($"Stored blob: {blob.Name}");
    }

    public void RestoredBlob(Blob blob)
    {
        Console.Error.WriteLine($"Restored blob: {blob.Name}");
    }

    public void BlobValid(Blob blob, bool valid)
    {
        Console.Error.WriteLine(valid
            ? $"Valid blob: {blob.Name}"
            : $"Invalid blob: {blob.Name}");
    }

    public void CopiedChunk(string chunkId)
    {
        Console.Error.WriteLine($"Copied chunk: {chunkId}");
    }

    public void RemovedChunk(string chunkId)
    {
        Console.Error.WriteLine($"Removed chunk: {chunkId}");
    }

    public void CopiedSnapshot(int snapshotId)
    {
        Console.Error.WriteLine($"Copied snapshot: #{snapshotId}");
    }

    public void StoredSnapshot(int snapshotId)
    {
        Console.Error.WriteLine($"Stored snapshot: #{snapshotId}");
    }

    public void RestoredSnapshot(int snapshotId)
    {
        Console.Error.WriteLine($"Restored snapshot: #{snapshotId}");
    }

    public void RemovedSnapshot(int snapshotId)
    {
        Console.Error.WriteLine($"Removed snapshot: #{snapshotId}");
    }

    public void SnapshotValid(int snapshotId, bool valid)
    {
        Console.Error.WriteLine(valid
            ? $"Valid snapshot: #{snapshotId}"
            : $"Invalid snapshot: #{snapshotId}");
    }
}
