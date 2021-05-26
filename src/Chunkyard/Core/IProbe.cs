using System;

namespace Chunkyard.Core
{
    /// <summary>
    /// Defines a set of events which are created while using an instance of a
    /// <see crefa="SnapshotStore"/>.
    /// </summary>
    public interface IProbe
    {
        void StoredBlob(BlobReference blobReference);

        void RetrievedBlob(BlobReference blobReference);

        void BlobValid(BlobReference blobReference, bool valid);

        void CopiedContent(Uri contentUri);

        void RemovedContent(Uri contentUri);

        void CopiedSnapshot(int snapshotId);

        void StoredSnapshot(int snapshotId);

        void RemovedSnapshot(int snapshotId);

        void SnapshotValid(int snapshotId, bool valid);
    }
}
