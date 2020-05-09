using System;
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
            RetrieveConfig retrieveConfig,
            Stream outputStream);

        T RetrieveContent<T>(
            ContentReference contentReference,
            RetrieveConfig retrieveConfig) where T : notnull;

        ContentReference StoreContent(
            Stream inputStream,
            StoreConfig storeConfig);

        ContentReference StoreContent<T>(T value, StoreConfig storeConfig)
            where T : notnull;

        int? FetchLogPosition();

        int AppendToLog<T>(T value, int? currentLogPosition)
            where T : notnull;

        T RetrieveFromLog<T>(int logPosition) where T : notnull;
    }
}
