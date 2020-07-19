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
            var repository = new MemoryRepository();
            var contentStore = CreateContentStore(repository);

            var contentReference = contentStore.StoreContentObject(
                "some text",
                "with some name");

            foreach (var uri in repository.ListUris())
            {
                repository.RemoveUri(uri);
            }

            Assert.False(contentStore.ContentExists(contentReference));
        }

        [Fact]
        public static void ContentValid_Detects_Corrupted_Content()
        {
            var repository = new MemoryRepository();
            var contentStore = CreateContentStore(repository);

            var contentReference = contentStore.StoreContentObject(
                "some text",
                "with some name");

            var wrongData = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

            foreach (var uri in repository.ListUris().ToArray())
            {
                repository.StoreUri(uri, wrongData);
            }

            Assert.False(contentStore.ContentValid(contentReference));
        }

        [Fact]
        public static void Append_And_RetrieveFromLog_Return_Reference()
        {
            var repository = new MemoryRepository();
            var contentStore = CreateContentStore(repository);
            var chunkReference = new ChunkReference(
                new Uri("sha256://abcdef123456"),
                new byte[] { 0xFF });

            var contentReference = new ContentReference(
                "some reference",
                AesGcmCrypto.GenerateNonce(),
                new[] { chunkReference });

            var logId = Guid.NewGuid();

            var firstLogPosition = contentStore.AppendToLog(
                logId,
                contentReference,
                0);

            var secondLogPosition = contentStore.AppendToLog(
                logId,
                contentReference,
                firstLogPosition);

            Assert.Equal(secondLogPosition, repository.FetchLogPosition());
            Assert.Equal(2, repository.ListLogPositions().Count());

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
