using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Chunkyard.Core
{
    public static class IContentStoreExtensions
    {
        public static byte[] RetrieveBytes<T>(this IContentStore<T> store, T contentRef) where T : IContentRef
        {
            using var memoryStream = new MemoryStream();
            store.Retrieve(memoryStream, contentRef);

            return memoryStream.ToArray();
        }

        public static string RetrieveUtf8<T>(this IContentStore<T> store, T contentRef) where T : IContentRef
        {
            return Encoding.UTF8.GetString(store.RetrieveBytes(contentRef));
        }

        public static T StoreBytes<T>(this IContentStore<T> store, byte[] data, HashAlgorithmName hashAlgorithmName, string contentName) where T : IContentRef
        {
            using var memoryStream = new MemoryStream(data);

            return store.Store(
                memoryStream,
                hashAlgorithmName,
                contentName);
        }

        public static T StoreUtf8<T>(this IContentStore<T> store, string text, HashAlgorithmName hashAlgorithmName, string contentName) where T : IContentRef
        {
            return store.StoreBytes(
                Encoding.UTF8.GetBytes(text),
                hashAlgorithmName,
                contentName);
        }

        public static void ThrowIfInvalid<T>(this IContentStore<T> store, T contentRef) where T : IContentRef
        {
            if (!store.Valid(contentRef))
            {
                throw new ChunkyardException($"Invalid content: {contentRef.Name}");
            }
        }
    }
}
