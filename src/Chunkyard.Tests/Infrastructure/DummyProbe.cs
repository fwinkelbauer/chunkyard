using System;
using Chunkyard.Core;

namespace Chunkyard.Tests.Infrastructure
{
    internal class DummyProbe : IProbe
    {
        public void StoredBlob(BlobReference blobReference)
        {
        }

        public void RetrievedBlob(BlobReference blobReference)
        {
        }

        public void BlobExists(BlobReference blobReference)
        {
        }

        public void BlobMissing(BlobReference blobReference)
        {
        }

        public void BlobValid(BlobReference blobReference)
        {
        }

        public void BlobInvalid(BlobReference blobReference)
        {
        }

        public void CopiedChunk(Uri contentUri)
        {
        }

        public void RemovedChunk(Uri contentUri)
        {
        }

        public void CopiedSnapshot(int snapshotId)
        {
        }

        public void StoredSnapshot(int snapshotId)
        {
        }

        public void RemovedSnapshot(int snapshotId)
        {
        }
    }
}
