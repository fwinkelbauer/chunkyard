using System.Collections.Generic;
using System.IO;

namespace Chunkyard.Core
{
    /// <summary>
    /// Defines a contract to read and write Blobs.
    /// </summary>
    public interface IBlobSystem
    {
        bool BlobExists(string blobName);

        IReadOnlyCollection<Blob> FetchBlobs(Fuzzy excludeFuzzy);

        Stream OpenRead(string blobName);

        Blob FetchMetadata(string blobName);

        Stream OpenWrite(Blob blob);
    }
}
