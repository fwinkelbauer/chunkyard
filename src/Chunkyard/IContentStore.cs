using System;
using System.Collections.Generic;
using System.IO;

namespace Chunkyard
{
    /// <summary>
    /// Defines a contract to store data and return a reference to retrieve that
    /// stored data.
    /// </summary>
    internal interface IContentStore
    {
        public IRepository Repository { get; }

        void RetrieveContent(
            ContentReference contentReference,
            Stream outputStream);

        T RetrieveContent<T>(ContentReference contentReference)
            where T : notnull;

        ContentReference StoreContent(Stream inputStream, string contentName);

        ContentReference StoreContent(
            Stream inputStream,
            ContentReference previousContentReference);

        ContentReference StoreContent<T>(T value, string contentName)
            where T : notnull;

        ContentReference StoreContent<T>(
            T value,
            ContentReference previousContentReference)
            where T : notnull;

        bool ContentExists(ContentReference contentReference);

        bool ContentValid(ContentReference contentReference);

        int? FetchLogPosition();

        int AppendToLog(
            ContentReference contentReference,
            int? currentLogPosition);

        LogReference RetrieveFromLog(int logPosition);

        IEnumerable<int> ListLogPositions();
    }
}
