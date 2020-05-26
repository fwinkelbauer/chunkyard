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

        [Fact]
        public static void ContentExists_Detects_Missing_Content()
        {
            var contentStore = CreateContentStore();

            var contentReference = contentStore.StoreContentObject<string>(
                "some text",
                "with some name");

            foreach (var uri in contentStore.Repository.ListUris())
            {
                contentStore.Repository.RemoveUri(uri);
            }

            Assert.False(contentStore.ContentExists(contentReference));
        }

        [Fact]
        public static void ContentValid_Detects_Corrupted_Content()
        {
            var contentStore = CreateContentStore();

            var contentReference = contentStore.StoreContentObject<string>(
                "some text",
                "with some name");

            var wrongData = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

            foreach (var uri in contentStore.Repository.ListUris().ToArray())
            {
                contentStore.Repository.StoreUri(uri, wrongData);
            }

            Assert.False(contentStore.ContentValid(contentReference));
        }

        [Fact]
        public static void Append_And_RetrieveFromLog_Return_Reference()
        {
            var contentStore = CreateContentStore();
            var chunkReference = new ChunkReference(
                new Uri("sha256://abcdef123456"),
                new byte[] { 0xFF });

            var contentReference = new ContentReference(
                "some reference",
                AesGcmCrypto.GenerateNonce(),
                new[] { chunkReference });

            Assert.Null(contentStore.FetchLogPosition());

            var firstLogPosition = contentStore.AppendToLog(
                contentReference,
                0);

            var secondLogPosition = contentStore.AppendToLog(
                contentReference,
                firstLogPosition);

            Assert.Equal(secondLogPosition, contentStore.FetchLogPosition());
            Assert.Equal(2, contentStore.ListLogPositions().Count());

            Assert.Equal(
                contentReference,
                contentStore.RetrieveFromLog(firstLogPosition).ContentReference);

            Assert.Equal(
                contentReference,
                contentStore.RetrieveFromLog(secondLogPosition).ContentReference);
        }

        private static ContentStore CreateContentStore()
        {
            return new ContentStore(
                new MemoryRepository(),
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
