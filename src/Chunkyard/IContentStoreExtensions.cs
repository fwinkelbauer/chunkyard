using System.IO;

namespace Chunkyard
{
    /// <summary>
    /// A set of extension methods to work with <see cref="IContentStore"/>.
    /// </summary>
    public static class IContentStoreExtensions
    {
        public static T RetrieveContentObject<T>(
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

        public static (ContentReference ContentReference, bool IsNewContent) StoreBlob(
            this IContentStore store,
            Stream inputStream,
            string contentName,
            byte[] nonce)
        {
            store.EnsureNotNull(nameof(store));

            return store.StoreContent(
                inputStream,
                contentName,
                nonce,
                ContentType.Blob);
        }

        public static (ContentReference ContentReference, bool IsNewContent) StoreDocument<T>(
            this IContentStore store,
            T value,
            string contentName,
            byte[] nonce)
            where T : notnull
        {
            store.EnsureNotNull(nameof(store));

            using var memoryStream = new MemoryStream(
                DataConvert.ToBytes(value));

            return store.StoreContent(
                memoryStream,
                contentName,
                nonce,
                ContentType.Document);
        }
    }
}
