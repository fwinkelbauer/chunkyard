using System.IO;

namespace Chunkyard.Core
{
    /// <summary>
    /// Defines a contract to store data and return a reference to retrieve that
    /// stored data.
    /// </summary>
    public interface IContentStore
    {
        IRepository Repository { get; }

        void RetrieveContent(
            ContentReference contentReference,
            byte[] key,
            Stream outputStream);

        ContentReference StoreContent(
            Stream inputStream,
            string contentName,
            byte[] key,
            byte[] nonce,
            ContentType type,
            out bool isNewContent);

        bool ContentExists(ContentReference contentReference);

        bool ContentValid(ContentReference contentReference);

        int AppendToLog(
            int newLogPosition,
            LogReference logReference);

        LogReference RetrieveFromLog(int logPosition);
    }
}
