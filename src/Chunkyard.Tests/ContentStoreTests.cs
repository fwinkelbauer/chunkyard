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

            var result = contentStore.StoreContent(
                inputStream,
                contentName);

            using var outputStream = new MemoryStream();
            contentStore.RetrieveContent(
                result.ContentReference,
                outputStream);

            var actualBytes = outputStream.ToArray();

            Assert.True(result.NewContent);
            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(contentName, result.ContentReference.Name);
            Assert.True(contentStore.ContentExists(result.ContentReference));
            Assert.True(contentStore.ContentValid(result.ContentReference));
        }

        [Fact]
        public static void Store_Indicates_NewContent_If_Stored_Twice()
        {
            var contentStore = CreateContentStore();

            var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var contentName = "some data";

            using var firstStream = new MemoryStream(bytes);
            var firstResult = contentStore.StoreContent(
                firstStream,
                contentName);

            using var secondStream = new MemoryStream(bytes);
            var secondResult = contentStore.StoreContent(
                secondStream,
                firstResult.ContentReference);

            Assert.True(firstResult.NewContent);
            Assert.False(secondResult.NewContent);
        }

        [Fact]
        public static void Store_And_RetrieveContentObject_Return_Object()
        {
            var contentStore = CreateContentStore();

            var expectedText = "some text";
            var contentName = "some name";

            var result = contentStore.StoreContentObject(
                expectedText,
                contentName);

            var actualText = contentStore.RetrieveContentObject<string>(
                result.ContentReference);

            Assert.Equal(expectedText, actualText);
            Assert.Equal(contentName, result.ContentReference.Name);
            Assert.True(contentStore.ContentExists(result.ContentReference));
            Assert.True(contentStore.ContentValid(result.ContentReference));
        }

        [Fact]
        public static void ContentExists_Detects_Missing_Content()
        {
            var contentStore = CreateContentStore(
                new UnstoredMemoryRepository());

            var result = contentStore.StoreContentObject(
                "some text",
                "with some name");

            Assert.False(contentStore.ContentExists(result.ContentReference));
        }

        [Fact]
        public static void ContentValid_Detects_Corrupted_Content()
        {
            var contentStore = CreateContentStore(
                new CorruptedMemoryRepository());

            var result = contentStore.StoreContentObject(
                "some text",
                "with some name");

            Assert.False(contentStore.ContentValid(result.ContentReference));
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
                });

            var firstLogPosition = contentStore.AppendToLog(
                logId,
                contentReference,
                0);

            var secondLogPosition = contentStore.AppendToLog(
                logId,
                contentReference,
                firstLogPosition + 1);

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
                new FastCdc(
                    2 * 1024 * 1024,
                    4 * 1024 * 1024,
                    8 * 1024 * 1024),
                HashAlgorithmName.SHA256,
                new StaticPrompt());
        }

        private class CorruptedMemoryRepository : MemoryRepository
        {
            public override bool StoreValue(Uri contentUri, byte[] value)
            {
                return base.StoreValue(
                    contentUri,
                    new byte[] { 0xBA, 0xD0 });
            }
        }
    }
}
