using System.IO;
using Chunkyard.Core;

namespace Chunkyard
{
    interface IContentStore
    {
        IRepository Repository { get; }

        void RetrieveContent(ContentReference contentReference, Stream stream, byte[] key);

        ContentReference StoreContent(Stream stream, string contentName, byte[] nonce, byte[] key, ChunkyardConfig config);
    }
}
