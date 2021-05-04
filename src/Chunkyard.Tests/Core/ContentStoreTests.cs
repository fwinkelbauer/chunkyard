using System;
using System.IO;
using Chunkyard.Core;
using Chunkyard.Tests.Infrastructure;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class ContentStoreTests
    {
        [Fact]
        public static void Store_And_RetrieveBlob_Return_Data()
        {
            var contentStore = CreateContentStore();

            var expectedBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            using var inputStream = new MemoryStream(expectedBytes);
            var blob = CreateBlob("some data");
            var key = CreateKey();

            var blobReference = contentStore.StoreBlob(
                blob,
                key,
                AesGcmCrypto.GenerateNonce(),
                inputStream);

            using var outputStream = new MemoryStream();
            contentStore.RetrieveBlob(
                blobReference,
                key,
                outputStream);

            var actualBytes = outputStream.ToArray();

            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(blob.Name, blobReference.Name);
            Assert.Equal(blob.CreationTimeUtc, blobReference.CreationTimeUtc);
            Assert.Equal(blob.LastWriteTimeUtc, blobReference.LastWriteTimeUtc);
            Assert.True(contentStore.ContentExists(blobReference));
            Assert.True(contentStore.ContentValid(blobReference));
        }

        [Fact]
        public static void Store_And_RetrieveDocument_Return_Object()
        {
            var contentStore = CreateContentStore();

            var expectedText = "some text";
            var key = CreateKey();

            var documentReference = contentStore.StoreDocument(
                expectedText,
                key,
                AesGcmCrypto.GenerateNonce());

            var actualText = contentStore.RetrieveDocument<string>(
                documentReference,
                key);

            Assert.Equal(expectedText, actualText);
            Assert.True(contentStore.ContentExists(documentReference));
            Assert.True(contentStore.ContentValid(documentReference));
        }

        [Fact]
        public static void ContentExists_Detects_Missing_Content()
        {
            var uriRepository = CreateUriRepository();
            var contentStore = CreateContentStore(uriRepository);

            var documentReference = contentStore.StoreDocument(
                "some text",
                CreateKey(),
                AesGcmCrypto.GenerateNonce());

            uriRepository.RemoveValues(uriRepository.ListKeys());

            Assert.False(contentStore.ContentExists(documentReference));
            Assert.False(contentStore.ContentValid(documentReference));
        }

        [Fact]
        public static void ContentValid_Detects_Corrupted_Content()
        {
            var uriRrepository = CreateUriRepository();
            var contentStore = CreateContentStore(uriRrepository);

            var documentReference = contentStore.StoreDocument(
                "some text",
                CreateKey(),
                AesGcmCrypto.GenerateNonce());

            uriRrepository.CorruptValues(uriRrepository.ListKeys());

            Assert.True(contentStore.ContentExists(documentReference));
            Assert.False(contentStore.ContentValid(documentReference));
        }

        private static ContentStore CreateContentStore(
            IRepository<Uri>? uriRepository = null)
        {
            return new ContentStore(
                uriRepository ?? CreateUriRepository(),
                new FastCdc(),
                Id.AlgorithmSHA256);
        }

        private static IRepository<Uri> CreateUriRepository()
        {
            return new MemoryRepository<Uri>();
        }

        private static byte[] CreateKey()
        {
            return AesGcmCrypto.PasswordToKey(
                "test",
                AesGcmCrypto.GenerateSalt(),
                AesGcmCrypto.Iterations);
        }

        private static Blob CreateBlob(string name)
        {
            return new Blob(
                name,
                DateTime.UtcNow,
                DateTime.UtcNow);
        }
    }
}
