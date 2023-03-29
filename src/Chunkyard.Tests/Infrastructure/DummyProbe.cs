namespace Chunkyard.Tests.Infrastructure;

internal sealed class DummyProbe : IProbe
{
    public void StoredBlob(Blob blob)
    {
    }

    public void RestoredBlob(Blob blob)
    {
    }

    public void BlobValid(Blob blob, bool valid)
    {
    }

    public void CopiedChunk(string chunkId)
    {
    }

    public void RemovedChunk(string chunkId)
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
