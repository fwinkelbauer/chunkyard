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
            Assert.True(blobReference.ContentUris.Count > 1);
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
            var repository = CreateUriRepository();
            var contentStore = CreateContentStore(repository);

            var documentReference = contentStore.StoreDocument(
                "some text",
                CreateKey(),
                AesGcmCrypto.GenerateNonce());

            repository.RemoveValues(repository.ListKeys());

            Assert.False(contentStore.ContentExists(documentReference));
            Assert.False(contentStore.ContentValid(documentReference));
        }

        [Fact]
        public static void ContentValid_Detects_Corrupted_Content()
        {
            var repository = CreateUriRepository();
            var contentStore = CreateContentStore(repository);

            var documentReference = contentStore.StoreDocument(
                "some text",
                CreateKey(),
                AesGcmCrypto.GenerateNonce());

            repository.CorruptValues(repository.ListKeys());

            Assert.True(contentStore.ContentExists(documentReference));
            Assert.False(contentStore.ContentValid(documentReference));
        }

        [Fact]
        public static void RetrieveDocument_Throws_On_Invalid_Content()
        {
            var repository = CreateUriRepository();
            var contentStore = CreateContentStore(repository);
            var key = CreateKey();

            var documentReference = contentStore.StoreDocument(
                "some text",
                key,
                AesGcmCrypto.GenerateNonce());

            repository.CorruptValues(repository.ListKeys());

            Assert.Throws<ChunkyardException>(
                () => contentStore.RetrieveDocument<string>(
                    documentReference,
                    key));
        }

        [Fact]
        public static void RetrieveDocument_Throws_Given_Wrong_Key()
        {
            var repository = CreateUriRepository();
            var contentStore = CreateContentStore(repository);

            var documentReference = contentStore.StoreDocument(
                "some text",
                CreateKey(),
                AesGcmCrypto.GenerateNonce());

            Assert.Throws<ChunkyardException>(
                () => contentStore.RetrieveDocument<string>(
                    documentReference,
                    CreateKey()));
        }

        [Fact]
        public static void Copy_Copies_Everything()
        {
            var sourceRepository = CreateUriRepository();
            var destinationRepository = CreateUriRepository();
            var contentStore = CreateContentStore(sourceRepository);

            contentStore.StoreDocument(
                "some text",
                CreateKey(),
                AesGcmCrypto.GenerateNonce());

            contentStore.Copy(destinationRepository);

            Assert.Equal(
                sourceRepository.ListKeys(),
                destinationRepository.ListKeys());
        }

        [Fact]
        public static void Copy_Throws_On_Invalid_Content()
        {
            var sourceRepository = CreateUriRepository();
            var destinationRepository = CreateUriRepository();
            var contentStore = CreateContentStore(sourceRepository);

            contentStore.StoreDocument(
                "some text",
                CreateKey(),
                AesGcmCrypto.GenerateNonce());

            sourceRepository.CorruptValues(sourceRepository.ListKeys());

            Assert.Throws<ChunkyardException>(
                () => contentStore.Copy(CreateUriRepository()));
        }

        [Fact]
        public static void RemoveExcept_Removes_Uris()
        {
            var repository = CreateUriRepository();
            var contentStore = CreateContentStore(repository);

            var documentReference = contentStore.StoreDocument(
                "some text",
                CreateKey(),
                AesGcmCrypto.GenerateNonce());

            contentStore.StoreDocument(
                "some text",
                CreateKey(),
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
                repository ?? CreateUriRepository(),
                fastCdc ?? new FastCdc(),
                Id.AlgorithmSHA256,
                new DummyProbe());
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
