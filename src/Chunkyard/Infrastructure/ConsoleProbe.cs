using System;
using Chunkyard.Core;

namespace Chunkyard.Infrastructure
{
    /// <summary>
    /// An implementation of <see cref="IProbe"/> which writes to the console.
    /// </summary>
    internal class ConsoleProbe : IProbe
    {
        public void StoredBlob(string name)
        {
            Console.WriteLine($"Stored blob: {name}");
        }

        public void RetrievedBlob(string name)
        {
            Console.WriteLine($"Restored blob: {name}");
        }

        public void BlobExists(string name)
        {
            Console.WriteLine($"Existing blob: {name}");
        }

        public void BlobMissing(string name)
        {
            Console.WriteLine($"Missing blob: {name}");
        }

        public void BlobValid(string name)
        {
            Console.WriteLine($"Valid blob: {name}");
        }

        public void BlobInvalid(string name)
        {
            Console.WriteLine($"Invalid blob: {name}");
        }

        public void CopiedContent(Uri contentUri)
        {
            Console.WriteLine($"Copied content: {contentUri}");
        }

        public void RemovedContent(Uri contentUri)
        {
            Console.WriteLine($"Removed content: {contentUri}");
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
