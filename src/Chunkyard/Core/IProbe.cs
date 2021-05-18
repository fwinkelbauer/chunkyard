using System;

namespace Chunkyard.Core
{
    /// <summary>
    /// Defines a set of events which are created while using an instance of a
    /// <see crefa="SnapshotStore"/>.
    /// </summary>
    public interface IProbe
    {
        void StoredContent(IContentReference contentReference);

        void RetrievedContent(IContentReference contentReference);

        void ContentExists(IContentReference contentReference);

        void ContentMissing(IContentReference contentReference);

        void ContentValid(IContentReference contentReference);

        void ContentInvalid(IContentReference contentReference);

        void CopiedChunk(Uri contentUri);

        void RemovedChunk(Uri contentUri);

        void CopiedSnapshot(int snapshotId);

        void StoredSnapshot(int snapshotId);

        void RemovedSnapshot(int snapshotId);
    }
}
