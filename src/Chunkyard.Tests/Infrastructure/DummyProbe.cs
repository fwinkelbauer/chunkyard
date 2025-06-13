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

    public void StoredSnapshot(int snapshotId)
    {
    }

    public void RestoredSnapshot(int snapshotId)
    {
    }

    public void SnapshotValid(int snapshotId, bool valid)
    {
    }

    public void RemovedSnapshot(int snapshotId)
    {
    }
}
