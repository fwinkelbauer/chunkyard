using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Chunkyard
{
    /// <summary>
    /// A set of extension methods to work with <see cref="IContentStore"/>.
    /// </summary>
    public static class IContentStoreExtensions
    {
        public static T RetrieveDocument<T>(
            this IContentStore store,
            ContentReference contentReference)
            where T : notnull
        {
            store.EnsureNotNull(nameof(store));

            using var memoryStream = new MemoryStream();

            store.RetrieveContent(
                contentReference,
                memoryStream);

            return DataConvert.ToObject<T>(memoryStream.ToArray());
        }

        public static ContentReference StoreBlob(
            this IContentStore store,
            Stream inputStream,
            string contentName,
            byte[] nonce,
            out bool newContent)
        {
            store.EnsureNotNull(nameof(store));

            return store.StoreContent(
                inputStream,
                contentName,
                nonce,
                ContentType.Blob,
                out newContent);
        }

        public static ContentReference StoreDocument<T>(
            this IContentStore store,
            T value,
            string contentName,
            byte[] nonce,
            out bool newContent)
            where T : notnull
        {
            store.EnsureNotNull(nameof(store));

            using var memoryStream = new MemoryStream(
                DataConvert.ToBytes(value));

            return store.StoreContent(
                memoryStream,
                contentName,
                nonce,
                ContentType.Document,
                out newContent);
        }

        public static Uri[] ListUris(
            this IContentStore store,
            ContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));

            return contentReference.Chunks
                .Select(c => c.ContentUri)
                .ToArray();
        }
    }
}
