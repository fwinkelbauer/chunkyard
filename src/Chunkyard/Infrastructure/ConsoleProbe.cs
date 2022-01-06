namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IProbe"/> which writes to the console.
/// </summary>
internal class ConsoleProbe : IProbe
{
    public void StoredBlob(BlobReference blobReference)
    {
        Console.WriteLine($"Stored blob: {blobReference.Blob.Name}");
    }

    public void RetrievedBlob(BlobReference blobReference)
    {
        Console.WriteLine($"Restored blob: {blobReference.Blob.Name}");
    }

    public void BlobValid(BlobReference blobReference, bool valid)
    {
        Console.WriteLine(valid
            ? $"Valid blob: {blobReference.Blob.Name}"
            : $"Invalid blob: {blobReference.Blob.Name}");
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

    public void RemovedSnapshot(int snapshotId)
    {
        Console.WriteLine($"Removed snapshot: #{snapshotId}");
    }
}
