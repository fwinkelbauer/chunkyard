namespace Chunkyard.Tests.Infrastructure;

internal class DummyProbe : IProbe
{
    public void StoredBlob(string blobName)
    {
    }

    public void RestoredBlob(string blobName)
    {
    }

    public void RemovedBlob(string blobName)
    {
    }

    public void BlobValid(string blobName, bool valid)
    {
    }

    public void CopiedContent(Uri contentUri)
    {
    }

    public void RemovedContent(Uri contentUri)
    {
    }

    public void CopiedSnapshot(int snapshotId)
    {
    }

    public void StoredSnapshot(int snapshotId)
    {
    }

    public void RestoredSnapshot(int snapshotId)
    {
    }

    public void RemovedSnapshot(int snapshotId)
    {
    }

    public void SnapshotValid(int snapshotId, bool valid)
    {
    }
}
