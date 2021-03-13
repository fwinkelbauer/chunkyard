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
            var repository = new MemoryRepository();
            var contentStore = CreateContentStore(repository);

            var documentReference = contentStore.StoreDocument(
                "some text",
                CreateKey(),
                AesGcmCrypto.GenerateNonce());

            RemoveValues(repository, repository.ListUris());

            Assert.False(contentStore.ContentExists(documentReference));
            Assert.False(contentStore.ContentValid(documentReference));
        }

        [Fact]
        public static void ContentValid_Detects_Corrupted_Content()
        {
            var repository = new MemoryRepository();
            var contentStore = CreateContentStore(repository);

            var documentReference = contentStore.StoreDocument(
                "some text",
                CreateKey(),
                AesGcmCrypto.GenerateNonce());

            CorruptValues(repository, repository.ListUris());

            Assert.True(contentStore.ContentExists(documentReference));
            Assert.False(contentStore.ContentValid(documentReference));
        }

        [Fact]
        public static void Append_And_RetrieveFromLog_Return_Reference()
        {
            var contentStore = CreateContentStore();

            var expectedLogReference = new LogReference(
                new DocumentReference(
                    AesGcmCrypto.GenerateNonce(),
                    new[] { new Uri("sha256://abcdef123456") }),
                AesGcmCrypto.GenerateSalt(),
                AesGcmCrypto.Iterations);

            var firstLogPosition = 0;
            var secondLogPosition = 1;

            contentStore.AppendToLog(
                firstLogPosition,
                expectedLogReference);

            contentStore.AppendToLog(
                secondLogPosition,
                expectedLogReference);

            var firstReference = contentStore.RetrieveFromLog(
                firstLogPosition);

            var secondReference = contentStore.RetrieveFromLog(
                secondLogPosition);

            Assert.Equal(
                expectedLogReference,
                firstReference);

            Assert.Equal(
                expectedLogReference,
                secondReference);
        }

        private static ContentStore CreateContentStore(
            IRepository? repository = null)
        {
            return new ContentStore(
                repository ?? new MemoryRepository(),
                new FastCdc(),
                HashAlgorithmName.SHA256);
        }

        private static byte[] CreateKey()
        {
            return AesGcmCrypto.PasswordToKey(
                "test",
                AesGcmCrypto.GenerateSalt(),
                AesGcmCrypto.Iterations);
        }

        private static void CorruptValues(
            IRepository repository,
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
            IRepository repository,
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
                DateTime.Now,
                DateTime.Now);
        }
    }
}
