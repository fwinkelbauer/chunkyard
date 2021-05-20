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
            var contentStore = CreateContentStore(
                fastCdc: new FastCdc(64, 256, 1024));

            var expectedBytes = AesGcmCrypto.GenerateRandomNumber(1025);
            using var inputStream = new MemoryStream(expectedBytes);
            var blob = new Blob("some data", DateTime.UtcNow);
            var key = CreateRandomKey();

            var blobReference = contentStore.StoreBlob(
                blob,
                key,
                AesGcmCrypto.GenerateNonce(),
                inputStream);

            using var outputStream = new MemoryStream();
            var retrievedBlob = contentStore.RetrieveBlob(
                blobReference,
                key,
                outputStream);

            Assert.Equal(expectedBytes, outputStream.ToArray());
            Assert.Equal(blob, retrievedBlob);
            Assert.Equal(blob, blobReference.ToBlob());
            Assert.True(blobReference.ContentUris.Count > 1);
            Assert.True(contentStore.ContentExists(blobReference));
            Assert.True(contentStore.ContentValid(blobReference));
        }

        [Fact]
        public static void Store_And_RetrieveDocument_Return_Object()
        {
            var contentStore = CreateContentStore();

            var expectedText = "some text";
            var key = CreateRandomKey();

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
            var repository = CreateRepository();
            var contentStore = CreateContentStore(repository);

            var documentReference = contentStore.StoreDocument(
                "some text",
                CreateRandomKey(),
                AesGcmCrypto.GenerateNonce());

            repository.RemoveValues(repository.ListKeys());

            Assert.False(contentStore.ContentExists(documentReference));
            Assert.False(contentStore.ContentValid(documentReference));
        }

        [Fact]
        public static void ContentValid_Detects_Invalid_Content()
        {
            var repository = CreateRepository();
            var contentStore = CreateContentStore(repository);

            var documentReference = contentStore.StoreDocument(
                "some text",
                CreateRandomKey(),
                AesGcmCrypto.GenerateNonce());

            repository.InvalidateValues(repository.ListKeys());

            Assert.True(contentStore.ContentExists(documentReference));
            Assert.False(contentStore.ContentValid(documentReference));
        }

        [Fact]
        public static void RetrieveDocument_Throws_On_Invalid_Content()
        {
            var repository = CreateRepository();
            var contentStore = CreateContentStore(repository);
            var key = CreateRandomKey();

            var documentReference = contentStore.StoreDocument(
                "some text",
                key,
                AesGcmCrypto.GenerateNonce());

            repository.InvalidateValues(repository.ListKeys());

            Assert.Throws<ChunkyardException>(
                () => contentStore.RetrieveDocument<string>(
                    documentReference,
                    key));
        }

        [Fact]
        public static void RetrieveDocument_Throws_Given_Wrong_Key()
        {
            var repository = CreateRepository();
            var contentStore = CreateContentStore(repository);

            var documentReference = contentStore.StoreDocument(
                "some text",
                CreateRandomKey(),
                AesGcmCrypto.GenerateNonce());

            Assert.Throws<ChunkyardException>(
                () => contentStore.RetrieveDocument<string>(
                    documentReference,
                    CreateRandomKey()));
        }

        [Fact]
        public static void Copy_Copies_Everything()
        {
            var sourceRepository = CreateRepository();
            var destinationRepository = CreateRepository();
            var contentStore = CreateContentStore(sourceRepository);

            contentStore.StoreDocument(
                "some text",
                CreateRandomKey(),
                AesGcmCrypto.GenerateNonce());

            contentStore.Copy(destinationRepository);

            Assert.Equal(
                sourceRepository.ListKeys(),
                destinationRepository.ListKeys());
        }

        [Fact]
        public static void Copy_Throws_On_Invalid_Content()
        {
            var sourceRepository = CreateRepository();
            var destinationRepository = CreateRepository();
            var contentStore = CreateContentStore(sourceRepository);

            contentStore.StoreDocument(
                "some text",
                CreateRandomKey(),
                AesGcmCrypto.GenerateNonce());

            sourceRepository.InvalidateValues(sourceRepository.ListKeys());

            Assert.Throws<ChunkyardException>(
                () => contentStore.Copy(CreateRepository()));
        }

        [Fact]
        public static void RemoveExcept_Removes_Uris()
        {
            var repository = CreateRepository();
            var contentStore = CreateContentStore(repository);

            var documentReference = contentStore.StoreDocument(
                "some text",
                CreateRandomKey(),
                AesGcmCrypto.GenerateNonce());

            contentStore.StoreDocument(
                "some text",
                CreateRandomKey(),
                AesGcmCrypto.GenerateNonce());

            contentStore.RemoveExcept(documentReference.ContentUris);

            Assert.Equal(
                documentReference.ContentUris,
                repository.ListKeys());
        }

        private static ContentStore CreateContentStore(
            IRepository<Uri>? repository = null,
            FastCdc? fastCdc = null)
        {
            return new ContentStore(
                repository ?? CreateRepository(),
                fastCdc ?? new FastCdc(),
                Id.AlgorithmSHA256,
                new DummyProbe());
        }

        private static IRepository<Uri> CreateRepository()
        {
            return new MemoryRepository<Uri>();
        }

        private static byte[] CreateRandomKey()
        {
            return AesGcmCrypto.PasswordToKey(
                "test",
                AesGcmCrypto.GenerateSalt(),
                AesGcmCrypto.Iterations);
        }
    }
}
