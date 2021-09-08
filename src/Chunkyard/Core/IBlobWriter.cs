using System.IO;

namespace Chunkyard.Core
{
    /// <summary>
    /// Defines a contract to find, write and update Blobs.
    /// </summary>
    public interface IBlobWriter
    {
        Blob? FindBlob(string blobName);

        Stream OpenWrite(string blobName);

        void UpdateBlobMetadata(Blob blob);
    }
}
