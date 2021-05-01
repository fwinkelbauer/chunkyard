using System;
using Chunkyard.Core;

namespace Chunkyard.Tests.Infrastructure
{
    internal class DummyProbe : IProbe
    {
        public void StoredBlob(string name)
        {
        }

        public void RestoredBlob(string name)
        {
        }

        public void BlobExists(string name)
        {
        }

        public void BlobMissing(string name)
        {
        }

        public void BlobValid(string name)
        {
        }

        public void BlobInvalid(string name)
        {
        }

        public void CopiedContent(Uri contentUri)
        {
        }

        public void RemovedContent(Uri contentUri)
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
