using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public static void Store_And_RetrieveContent_Return_Data()
        {
            var contentStore = CreateContentStore();

            var expectedBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            using var inputStream = new MemoryStream(expectedBytes);

            var contentName = "some data";
            var key = GenerateKey();

            var contentReference = contentStore.StoreBlob(
                inputStream,
                contentName,
                key,
                AesGcmCrypto.GenerateNonce(),
                out var isNewContent);

            using var outputStream = new MemoryStream();
            contentStore.RetrieveContent(
                contentReference,
                key,
                outputStream);

            var actualBytes = outputStream.ToArray();

            Assert.True(isNewContent);
            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(contentName, contentReference.Name);
            Assert.True(contentStore.ContentExists(contentReference));
            Assert.True(contentStore.ContentValid(contentReference));
        }

        [Fact]
        public static void Store_Detects_Already_Stored_Content_Using_Same_Nonce()
        {
            var contentStore = CreateContentStore();

            var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            using var inputStream1 = new MemoryStream(bytes);
            using var inputStream2 = new MemoryStream(bytes);

            var contentName = "some data";
            var key = GenerateKey();
            var nonce = AesGcmCrypto.GenerateNonce();

            contentStore.StoreBlob(
                inputStream1,
                contentName,
                key,
                nonce,
                out var isNewContent1);

            contentStore.StoreBlob(
                inputStream2,
                contentName,
                key,
                nonce,
                out var isNewContent2);

            Assert.True(isNewContent1);
            Assert.False(isNewContent2);
        }

        [Fact]
        public static void Store_Does_Not_Detect_Same_Content_Using_Other_Nonce()
        {
            var contentStore = CreateContentStore();

            var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            using var inputStream1 = new MemoryStream(bytes);
            using var inputStream2 = new MemoryStream(bytes);

            var contentName = "some data";
            var key = GenerateKey();

            contentStore.StoreBlob(
                inputStream1,
                contentName,
                key,
                AesGcmCrypto.GenerateNonce(),
                out var isNewContent1);

            contentStore.StoreBlob(
                inputStream2,
                contentName,
                key,
                AesGcmCrypto.GenerateNonce(),
                out var isNewContent2);

            Assert.True(isNewContent1);
            Assert.True(isNewContent2);
        }

        [Fact]
        public static void Store_And_RetrieveDocument_Return_Object()
        {
            var contentStore = CreateContentStore();

            var expectedText = "some text";
            var contentName = "some name";
            var key = GenerateKey();

            var contentReference = contentStore.StoreDocument(
                expectedText,
                contentName,
                key,
                AesGcmCrypto.GenerateNonce(),
                out _);

            var actualText = contentStore.RetrieveDocument<string>(
                contentReference,
                key);

            Assert.Equal(expectedText, actualText);
            Assert.Equal(contentName, contentReference.Name);
            Assert.True(contentStore.ContentExists(contentReference));
            Assert.True(contentStore.ContentValid(contentReference));
        }

        [Fact]
        public static void ContentExists_Detects_Missing_Content()
        {
            var repository = new MemoryRepository();
            var contentStore = CreateContentStore(repository);

            var contentReference = contentStore.StoreDocument(
                "some text",
                "with some name",
                GenerateKey(),
                AesGcmCrypto.GenerateNonce(),
                out _);

            RemoveValues(repository, repository.ListUris());

            Assert.False(contentStore.ContentExists(contentReference));
            Assert.False(contentStore.ContentValid(contentReference));
        }

        [Fact]
        public static void ContentValid_Detects_Corrupted_Content()
        {
            var repository = new MemoryRepository();
            var contentStore = CreateContentStore(repository);

            var contentReference = contentStore.StoreDocument(
                "some text",
                "with some name",
                GenerateKey(),
                AesGcmCrypto.GenerateNonce(),
                out _);

            CorruptValues(repository, repository.ListUris());

            Assert.True(contentStore.ContentExists(contentReference));
            Assert.False(contentStore.ContentValid(contentReference));
        }

        [Fact]
        public static void Append_And_RetrieveFromLog_Return_Reference()
        {
            var contentStore = CreateContentStore();

            var expectedLogReference = new LogReference(
                new ContentReference(
                    "some reference",
                    AesGcmCrypto.GenerateNonce(),
                    ImmutableArray.Create(
                        new ChunkReference(
                            new Uri("sha256://abcdef123456"),
                            new byte[] { 0xFF })),
                    ContentType.Blob),
                AesGcmCrypto.GenerateSalt(),
                AesGcmCrypto.Iterations);

            var firstLogPosition = contentStore.AppendToLog(
                0,
                expectedLogReference);

            var secondLogPosition = contentStore.AppendToLog(
                firstLogPosition + 1,
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

        private static byte[] GenerateKey()
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
    }
}
