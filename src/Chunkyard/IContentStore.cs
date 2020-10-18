using System;
using System.IO;

namespace Chunkyard
{
    /// <summary>
    /// Defines a contract to store data and return a reference to retrieve that
    /// stored data.
    /// </summary>
    public interface IContentStore
    {
        int? CurrentLogPosition { get; }

        void RetrieveContent(
            ContentReference contentReference,
            Stream outputStream);

        ContentReference StoreContent(
            Stream inputStream,
            string contentName,
            byte[] nonce,
            ContentType type,
            out bool newContent);

        bool ContentExists(ContentReference contentReference);

        bool ContentValid(ContentReference contentReference);

        int AppendToLog(
            Guid logId,
            ContentReference contentReference,
            int newLogPosition);

        LogReference RetrieveFromLog(int logPosition);
    }
}
