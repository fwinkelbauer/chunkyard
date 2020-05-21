using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace Chunkyard.Tests
{
    public static class ContentStoreTests
    {
        [Fact]
        public static void Store_And_RetrieveContent_Return_Data()
        {
            var contentStore = CreateContentStore();

            var expectedBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var contentName = "some data";
            using var inputStream = new MemoryStream(expectedBytes);

            var contentReference = contentStore.StoreContent(
                inputStream,
                contentName);

            using var outputStream = new MemoryStream();
            contentStore.RetrieveContent(
                contentReference,
                outputStream);

            var actualBytes = outputStream.ToArray();

            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(contentName, contentReference.Name);
            Assert.True(contentStore.ContentExists(contentReference));
            Assert.True(contentStore.ContentValid(contentReference));
        }

        [Fact]
        public static void Store_And_RetrieveContentObject_ReturnObject()
        {
            var contentStore = CreateContentStore();

            var expectedText = "some text";
            var contentName = "some name";

            var contentReference = contentStore.StoreContentObject<string>(
                expectedText,
                contentName);

            var actualText = contentStore.RetrieveContentObject<string>(
                contentReference);

            Assert.Equal(expectedText, actualText);
            Assert.Equal(contentName, contentReference.Name);
            Assert.True(contentStore.ContentExists(contentReference));
            Assert.True(contentStore.ContentValid(contentReference));
        }

        private static ContentStore CreateContentStore()
        {
            return CreateContentStore(new MemoryRepository());
        }

        private static ContentStore CreateContentStore(IRepository repository)
        {
            return new ContentStore(
                repository,
                new FastCdc(
                    2 * 1024 * 1024,
                    4 * 1024 * 1024,
                    8 * 1024 * 1024),
                HashAlgorithmName.SHA256,
                "secret password",
                AesGcmCrypto.GenerateSalt(),
                5);
        }
    }
}
