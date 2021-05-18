using System;
using Chunkyard.Core;

namespace Chunkyard.Tests.Infrastructure
{
    internal class DummyProbe : IProbe
    {
        public void StoredContent(IContentReference contentReference)
        {
        }

        public void RetrievedContent(IContentReference contentReference)
        {
        }

        public void ContentExists(IContentReference contentReference)
        {
        }

        public void ContentMissing(IContentReference contentReference)
        {
        }

        public void ContentValid(IContentReference contentReference)
        {
        }

        public void ContentInvalid(IContentReference contentReference)
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
