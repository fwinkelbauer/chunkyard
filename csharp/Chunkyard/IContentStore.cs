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
            KeyInformation key,
            Stream outputStream);

        T RetrieveContent<T>(
            ContentReference contentReference,
            KeyInformation key) where T : notnull;

        ContentReference StoreContent(
            Stream inputStream,
            KeyInformation key,
            string contentName);

        ContentReference StoreContent<T>(
            T value,
            KeyInformation key,
            string contentName) where T : notnull;

        bool ContentExists(ContentReference contentReference);

        bool ContentValid(ContentReference contentReference);

        int? FetchLogPosition();

        int AppendToLog(
            ContentReference contentReference,
            int? currentLogPosition);

        ContentReference RetrieveFromLog(int logPosition);

        IEnumerable<int> ListLogPositions();
    }
}
