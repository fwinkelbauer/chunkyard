using System.IO;

namespace Chunkyard.Core
{
    /// <summary>
    /// A set of extension methods to work with <see cref="IContentStore"/>.
    /// </summary>
    public static class IContentStoreExtensions
    {
        public static T RetrieveDocument<T>(
            this IContentStore store,
            ContentReference contentReference,
            byte[] key)
            where T : notnull
        {
            store.EnsureNotNull(nameof(store));

            using var memoryStream = new MemoryStream();

            store.RetrieveContent(
                contentReference,
                key,
                memoryStream);

            return DataConvert.ToObject<T>(memoryStream.ToArray());
        }

        public static ContentReference StoreBlob(
            this IContentStore store,
            Stream inputStream,
            string contentName,
            byte[] key,
            byte[] nonce,
            out bool isNewContent)
        {
            store.EnsureNotNull(nameof(store));

            return store.StoreContent(
                inputStream,
                contentName,
                key,
                nonce,
                ContentType.Blob,
                out isNewContent);
        }

        public static ContentReference StoreDocument<T>(
            this IContentStore store,
            T value,
            string contentName,
            byte[] key,
            byte[] nonce,
            out bool isNewContent)
            where T : notnull
        {
            store.EnsureNotNull(nameof(store));

            using var memoryStream = new MemoryStream(
                DataConvert.ToBytes(value));

            return store.StoreContent(
                memoryStream,
                contentName,
                key,
                nonce,
                ContentType.Document,
                out isNewContent);
        }
    }
}
