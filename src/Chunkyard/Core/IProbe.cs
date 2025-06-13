namespace Chunkyard.Core;

/// <summary>
/// Defines a set of events which are created while using an instance of a
/// <see cref="SnapshotStore"/>.
/// </summary>
public interface IProbe
{
    void StoredBlob(Blob blob);

    void RestoredBlob(Blob blob);

    void BlobValid(Blob blob, bool valid);

    void StoredSnapshot(int snapshotId);

    void RestoredSnapshot(int snapshotId);

    void SnapshotValid(int snapshotId, bool valid);

    void RemovedSnapshot(int snapshotId);
}
