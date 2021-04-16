using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
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

            RemoveValues(uriRepository, uriRepository.ListKeys());

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

            CorruptValues(uriRrepository, uriRrepository.ListKeys());

            Assert.True(contentStore.ContentExists(documentReference));
            Assert.False(contentStore.ContentValid(documentReference));
        }

        [Fact]
        public static void RemoveContent_Removes_Content()
        {
            var contentStore = CreateContentStore();

            var documentReference = contentStore.StoreDocument(
                "some text",
                CreateKey(),
                AesGcmCrypto.GenerateNonce());

            Assert.Equal(
                documentReference.ContentUris,
                contentStore.ListContentUris());

            foreach (var contentUri in documentReference.ContentUris)
            {
                contentStore.RemoveContent(contentUri);
            }

            Assert.Empty(contentStore.ListContentUris());
        }

        private static ContentStore CreateContentStore(
            IRepository<Uri>? uriRepository = null)
        {
            return new ContentStore(
                uriRepository ?? CreateUriRepository(),
                new FastCdc(),
                HashAlgorithmName.SHA256);
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

        private static void CorruptValues(
            IRepository<Uri> repository,
            IEnumerable<Uri> contentUris)
        {
            foreach (var contentUri in contentUris)
            {
                repository.RemoveValue(contentUri);
                repository.StoreValue(
                    contentUri,
                    new byte[] { 0xFF, 0xBA, 0xDD, 0xFF });
            }
        }

        private static void RemoveValues(
            IRepository<Uri> repository,
            IEnumerable<Uri> contentUris)
        {
            foreach (var contentUri in contentUris)
            {
                repository.RemoveValue(contentUri);
            }
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
