using System.Collections.Generic;
using System.IO;

namespace Chunkyard.Core
{
    /// <summary>
    /// Defines a contract to list and read Blobs.
    /// </summary>
    public interface IBlobReader
    {
        IReadOnlyCollection<Blob> FetchBlobs();

        Stream OpenRead(string blobName);
    }
}
