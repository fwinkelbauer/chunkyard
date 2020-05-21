using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace Chunkyard.Tests
{
    public static class ContentStoreTests
    {
        [Fact]
        public static void StoreContent_And_RetrieveContent_Return_Data()
        {
            var repository = new MemoryRepository();
            var contentStore = CreateContentStore(repository);

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
