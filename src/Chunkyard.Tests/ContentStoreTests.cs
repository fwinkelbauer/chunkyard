using System;
using System.IO;
using System.Linq;
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
        public static void Store_And_RetrieveContentObject_Return_Object()
        {
            var contentStore = CreateContentStore();

            var expectedText = "some text";
            var contentName = "some name";

            var contentReference = contentStore.StoreContentObject(
                expectedText,
                contentName);

            var actualText = contentStore.RetrieveContentObject<string>(
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
                new UnstoredMemoryRepository());

            var contentReference = contentStore.StoreContentObject(
                "some text",
                "with some name");

            Assert.False(contentStore.ContentExists(contentReference));
        }

        [Fact]
        public static void ContentValid_Detects_Corrupted_Content()
        {
            var contentStore = CreateContentStore(
                new CorruptedMemoryRepository());

            var contentReference = contentStore.StoreContentObject(
                "some text",
                "with some name");

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
