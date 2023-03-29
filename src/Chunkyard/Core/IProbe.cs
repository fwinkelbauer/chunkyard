namespace Chunkyard.Core;

/// <summary>
/// Defines a set of events which are created while using an instance of a
/// <see cref="SnapshotStore"/>.
/// </summary>
public interface IProbe
{
    void StoredBlob(Blob blob);

    void RestoredBlob(Blob blob);

    void RemovedBlob(Blob blob);

    void BlobValid(Blob blob, bool valid);

    void CopiedChunk(string chunkId);

    void RemovedChunk(string chunkId);

    void CopiedSnapshot(int snapshotId);

    void StoredSnapshot(int snapshotId);

    void RestoredSnapshot(int snapshotId);

    void RemovedSnapshot(int snapshotId);

    void SnapshotValid(int snapshotId, bool valid);
}
