﻿using System.IO;

namespace Chunkyard
{
    /// <summary>
    /// Defines a contract to store data and return a reference to retrieve that
    /// stored data.
    /// </summary>
    public interface IContentStore
    {
        IRepository Repository { get; }

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
            int newLogPosition,
            ContentReference contentReference);

        LogReference RetrieveFromLog(int logPosition);
    }
}
