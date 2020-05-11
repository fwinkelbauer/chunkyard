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
            byte[] key,
            Stream outputStream);

        T RetrieveContent<T>(
            ContentReference contentReference,
            byte[] key) where T : notnull;

        ContentReference StoreContent(
            Stream inputStream,
            byte[] key,
            string contentName);

        ContentReference StoreContent<T>(
            T value,
            byte[] key,
            string contentName) where T : notnull;

        bool ContentExists(ContentReference contentReference);

        bool ContentValid(ContentReference contentReference);

        int? FetchLogPosition();

        int AppendToLog<T>(T value, int? currentLogPosition)
            where T : notnull;

        T RetrieveFromLog<T>(int logPosition) where T : notnull;
    }
}
