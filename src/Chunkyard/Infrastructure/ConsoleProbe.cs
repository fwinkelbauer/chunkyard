using System;
using Chunkyard.Core;

namespace Chunkyard.Infrastructure
{
    /// <summary>
    /// An implementation of <see cref="IProbe"/> which writes to the console.
    /// </summary>
    internal class ConsoleProbe : IProbe
    {
        public void StoredContent(IContentReference contentReference)
        {
            if (contentReference is BlobReference blobReference)
            {
                Console.WriteLine($"Stored blob: {blobReference.Name}");
            }
        }

        public void RetrievedContent(IContentReference contentReference)
        {
            if (contentReference is BlobReference blobReference)
            {
                Console.WriteLine($"Restored blob: {blobReference.Name}");
            }
        }

        public void ContentExists(IContentReference contentReference)
        {
            if (contentReference is BlobReference blobReference)
            {
                Console.WriteLine($"Existing blob: {blobReference.Name}");
            }
        }

        public void ContentMissing(IContentReference contentReference)
        {
            if (contentReference is BlobReference blobReference)
            {
                Console.WriteLine($"Missing blob: {blobReference.Name}");
            }
        }

        public void ContentValid(IContentReference contentReference)
        {
            if (contentReference is BlobReference blobReference)
            {
                Console.WriteLine($"Valid blob: {blobReference.Name}");
            }
        }

        public void ContentInvalid(IContentReference contentReference)
        {
            if (contentReference is BlobReference blobReference)
            {
                Console.WriteLine($"Invalid blob: {blobReference.Name}");
            }
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
