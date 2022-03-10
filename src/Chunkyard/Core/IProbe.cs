namespace Chunkyard.Core;

/// <summary>
/// Defines a set of events which are created while using an instance of a
/// <see cref="SnapshotStore"/>.
/// </summary>
public interface IProbe
{
    void StoredBlob(string blobName);

    void RestoredBlob(string blobName);

    void RemovedBlob(string blobName);

    void BlobValid(string blobName, bool valid);

    void CopiedChunk(Uri chunkId);

    void RemovedChunk(Uri chunkId);

    void CopiedSnapshot(int snapshotId);

    void StoredSnapshot(int snapshotId);

    void RestoredSnapshot(int snapshotId);

    void RemovedSnapshot(int snapshotId);

    void SnapshotValid(int snapshotId, bool valid);
}
