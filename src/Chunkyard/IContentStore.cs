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

        (ContentReference ContentReference, bool IsNewContent) StoreContent(
            Stream inputStream,
            string contentName);

        void RegisterContent(ContentReference contentReference);

        bool ContentExists(ContentReference contentReference);

        bool ContentValid(ContentReference contentReference);

        int AppendToLog(
            Guid logId,
            ContentReference contentReference,
            int newLogPosition);

        LogReference RetrieveFromLog(int logPosition);
    }
}
