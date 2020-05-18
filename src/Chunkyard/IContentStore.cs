using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Chunkyard
{
    internal interface IContentStore
    {
        public Uri StoreUri { get; }

        void RetrieveContent(
            ContentReference contentReference,
            Stream outputStream);

        T RetrieveContent<T>(ContentReference contentReference)
            where T : notnull;

        ContentReference StoreContent(Stream inputStream, string contentName);

        ContentReference StoreContent<T>(T value, string contentName)
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
