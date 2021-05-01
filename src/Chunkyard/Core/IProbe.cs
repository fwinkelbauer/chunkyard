using System;

namespace Chunkyard.Core
{
    /// <summary>
    /// Defines a set of events which are created while using an instance of a
    /// <see crefa="SnapshotStore"/>.
    /// </summary>
    public interface IProbe
    {
        void StoredBlob(string name);

        void RestoredBlob(string name);

        void BlobExists(string name);

        void BlobMissing(string name);

        void BlobValid(string name);

        void BlobInvalid(string name);

        void CopiedContent(Uri contentUri);

        void RemovedContent(Uri contentUri);

        void CopiedSnapshot(int snapshotId);

        void StoredSnapshot(int snapshotId);

        void RemovedSnapshot(int snapshotId);
    }
}
