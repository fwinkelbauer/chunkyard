namespace Chunkyard.Core;

/// <summary>
/// Defines a set of events which are created while using an instance of a
/// <see cref="SnapshotStore"/>.
/// </summary>
public interface IProbe
{
    void StoredBlob(Blob blob);

    void RetrievedBlob(Blob blob);

    void RemovedBlob(Blob blob);

    void BlobValid(Blob blob, bool valid);

    void CopiedContent(Uri contentUri);

    void RemovedContent(Uri contentUri);

    void CopiedSnapshot(int snapshotId);

    void StoredSnapshot(int snapshotId);

    void RetrievedSnapshot(int snapshotId);

    void RemovedSnapshot(int snapshotId);

    void SnapshotValid(int snapshotId, bool valid);
}
