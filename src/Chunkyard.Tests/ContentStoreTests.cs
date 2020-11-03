using System;
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

            var contentReference = contentStore.StoreBlob(
                inputStream,
                contentName,
                AesGcmCrypto.GenerateNonce(),
                out var newContent);

            using var outputStream = new MemoryStream();
            contentStore.RetrieveContent(
                contentReference,
                outputStream);

            var actualBytes = outputStream.ToArray();

            Assert.True(newContent);
            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(contentName, contentReference.Name);
            Assert.True(contentStore.ContentExists(contentReference));
            Assert.True(contentStore.ContentValid(contentReference));
        }

        [Fact]
        public static void Store_Detects_Same_Content()
        {
            var contentStore = CreateContentStore();

            var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var contentName = "some data";
            var nonce = AesGcmCrypto.GenerateNonce();

            using var firstStream = new MemoryStream(bytes);
            var firstResult = contentStore.StoreBlob(
                firstStream,
                contentName,
                nonce,
                out var firstNewContent);

            using var secondStream = new MemoryStream(bytes);
            var secondResult = contentStore.StoreBlob(
                secondStream,
                contentName,
                nonce,
                out var secondNewContent);

            Assert.True(firstNewContent);
            Assert.False(secondNewContent);
        }

        [Fact]
        public static void Store_Does_Not_Detect_Same_Content_Using_Other_Nonce()
        {
            var contentStore = CreateContentStore();

            var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var contentName = "some data";

            using var firstStream = new MemoryStream(bytes);
            var firstResult = contentStore.StoreBlob(
                firstStream,
                contentName,
                AesGcmCrypto.GenerateNonce(),
                out var firstNewContent);

            using var secondStream = new MemoryStream(bytes);
            var secondResult = contentStore.StoreBlob(
                secondStream,
                contentName,
                AesGcmCrypto.GenerateNonce(),
                out var secondNewContent);

            Assert.True(firstNewContent);
            Assert.True(secondNewContent);
        }

        [Fact]
        public static void Store_And_RetrieveDocument_Return_Object()
        {
            var contentStore = CreateContentStore();

            var expectedText = "some text";
            var contentName = "some name";

            var contentReference = contentStore.StoreDocument(
                expectedText,
                contentName,
                AesGcmCrypto.GenerateNonce(),
                out _);

            var actualText = contentStore.RetrieveDocument<string>(
                contentReference);

            Assert.Equal(expectedText, actualText);
            Assert.Equal(contentName, contentReference.Name);
            Assert.True(contentStore.ContentExists(contentReference));
            Assert.True(contentStore.ContentValid(contentReference));
        }

        [Fact]
        public static void ContentExists_Detects_Missing_Content()
        {
            var contentStore = CreateContentStore(
                new UnstoredRepository());

            var contentReference = contentStore.StoreDocument(
                "some text",
                "with some name",
                AesGcmCrypto.GenerateNonce(),
                out _);

            Assert.False(contentStore.ContentExists(contentReference));
        }

        [Fact]
        public static void ContentValid_Detects_Corrupted_Content()
        {
            var contentStore = CreateContentStore(
                new CorruptedRepository());

            var contentReference = contentStore.StoreDocument(
                "some text",
                "with some name",
                AesGcmCrypto.GenerateNonce(),
                out _);

            Assert.False(contentStore.ContentValid(contentReference));
        }

        [Fact]
        public static void Append_And_RetrieveFromLog_Return_Reference()
        {
            var contentStore = CreateContentStore();

            var logId = Guid.NewGuid();
            var contentReference = new ContentReference(
                "some reference",
                AesGcmCrypto.GenerateNonce(),
                new[]
                {
                    new ChunkReference(
                        new Uri("sha256://abcdef123456"),
                        new byte[] { 0xFF })
                },
                ContentType.Blob);

            var firstLogPosition = contentStore.AppendToLog(
                logId,
                0,
                contentReference);

            var secondLogPosition = contentStore.AppendToLog(
                logId,
                firstLogPosition + 1,
                contentReference);

            var firstReference = contentStore.RetrieveFromLog(
                firstLogPosition);

            var secondReference = contentStore.RetrieveFromLog(
                secondLogPosition);

            Assert.Equal(
                contentReference,
                firstReference.ContentReference);

            Assert.Equal(
                contentReference,
                secondReference.ContentReference);

            Assert.Equal(
                secondLogPosition,
                contentStore.CurrentLogPosition);
        }

        private static ContentStore CreateContentStore(
            IRepository? repository = null)
        {
            return new ContentStore(
                repository ?? new MemoryRepository(),
                new FastCdc(),
                HashAlgorithmName.SHA256,
                new StaticPrompt());
        }

        private class CorruptedRepository : DecoratorRepository
        {
            public CorruptedRepository()
                : base(new MemoryRepository())
            {
            }

            public override bool StoreValue(Uri contentUri, byte[] value)
            {
                return base.StoreValue(
                    contentUri,
                    new byte[] { 0xBA, 0xD0 });
            }
        }
    }
}
