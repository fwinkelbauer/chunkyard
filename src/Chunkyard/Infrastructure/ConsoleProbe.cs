namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IProbe"/> which writes to the console.
/// </summary>
internal class ConsoleProbe : IProbe
{
    public void StoredBlob(Blob blob)
    {
        Console.WriteLine($"Stored blob: {blob.Name}");
    }

    public void RestoredBlob(Blob blob)
    {
        Console.WriteLine($"Restored blob: {blob.Name}");
    }

    public void RemovedBlob(Blob blob)
    {
        Console.WriteLine($"Removed blob: {blob.Name}");
    }

    public void BlobValid(Blob blob, bool valid)
    {
        Console.WriteLine(valid
            ? $"Valid blob: {blob.Name}"
            : $"Invalid blob: {blob.Name}");
    }

    public void CopiedContent(Uri contentUri)
    {
        Console.WriteLine($"Copied content: {contentUri}");
    }

    public void RemovedContent(Uri contentUri)
    {
        Console.WriteLine($"Removed content: {contentUri}");
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
