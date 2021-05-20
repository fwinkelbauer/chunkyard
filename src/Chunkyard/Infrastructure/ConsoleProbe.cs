using System;
using Chunkyard.Core;

namespace Chunkyard.Infrastructure
{
    /// <summary>
    /// An implementation of <see cref="IProbe"/> which writes to the console.
    /// </summary>
    internal class ConsoleProbe : IProbe
    {
        public void StoredBlob(BlobReference blobReference)
        {
            Console.WriteLine($"Stored blob: {blobReference.Name}");
        }

        public void RetrievedBlob(BlobReference blobReference)
        {
            Console.WriteLine($"Restored blob: {blobReference.Name}");
        }

        public void BlobExists(BlobReference blobReference)
        {
            Console.WriteLine($"Existing blob: {blobReference.Name}");
        }

        public void BlobMissing(BlobReference blobReference)
        {
            Console.WriteLine($"Missing blob: {blobReference.Name}");
        }

        public void BlobValid(BlobReference blobReference)
        {
            Console.WriteLine($"Valid blob: {blobReference.Name}");
        }

        public void BlobInvalid(BlobReference blobReference)
        {
            Console.WriteLine($"Invalid blob: {blobReference.Name}");
        }

        public void CopiedChunk(Uri contentUri)
        {
            Console.WriteLine($"Copied chunk: {contentUri}");
        }

        public void RemovedChunk(Uri contentUri)
        {
            Console.WriteLine($"Removed chunk: {contentUri}");
        }

        public void CopiedSnapshot(int snapshotId)
        {
            Console.WriteLine($"Copied snapshot: #{snapshotId}");
        }

        public void StoredSnapshot(int snapshotId)
        {
            Console.WriteLine($"Created snapshot: #{snapshotId}");
        }

        public void RemovedSnapshot(int snapshotId)
        {
            Console.WriteLine($"Removed snapshot: #{snapshotId}");
        }
    }
}
