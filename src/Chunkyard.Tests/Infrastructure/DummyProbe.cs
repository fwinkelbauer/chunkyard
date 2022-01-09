namespace Chunkyard.Tests.Infrastructure;

internal class DummyProbe : IProbe
{
    public void StoredBlob(Blob blob)
    {
    }

    public void RetrievedBlob(Blob blob)
    {
    }

    public void RemovedBlob(Blob blob)
    {
    }

    public void BlobValid(Blob blob, bool valid)
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

    public void RetrievedSnapshot(int snapshotId)
    {
    }

    public void RemovedSnapshot(int snapshotId)
    {
    }

    public void SnapshotValid(int snapshotId, bool valid)
    {
    }
}
