using System;
using System.Security.Cryptography;
using Chunkyard.Core;
using Chunkyard.Tests.Infrastructure;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class RepositoryExtensionsTests
    {
        [Fact]
        public static void StoreValue_Calculates_Same_Hash_For_Same_Input()
        {
            var repository = CreateUriRepository();
            var hashAlgorithmName = HashAlgorithmName.SHA256;
            var value = new byte[] { 0xFF };
            var expectedContentUri = new Uri("sha256://a8100ae6aa1940d0b663bb31cd466142ebbdbd5187131b92d93818987832eb89/");

            var actualContentUri1 = repository.StoreValue(
                hashAlgorithmName,
                value);

            var actualContentUri2 = repository.StoreValue(
                hashAlgorithmName,
                value);

            Assert.Equal(expectedContentUri, actualContentUri1);
            Assert.Equal(expectedContentUri, actualContentUri2);
        }

        [Fact]
        public static void RetrieveValueValid_Retrieves_Valid_Value()
        {
            var repository = CreateUriRepository();
            var expectedValue = new byte[] { 0xFF };

            var contentUri = repository.StoreValue(HashAlgorithmName.SHA256, expectedValue);

            Assert.Equal(
                expectedValue,
                repository.RetrieveValueValid(contentUri));
        }

        [Fact]
        public static void RetrieveValueValid_Throws_If_Value_Invalid()
        {
            var repository = CreateUriRepository();
            var contentUri = new Uri("sha256://badbadbad");
            var value = new byte[] { 0xFF };

            repository.StoreValue(contentUri, value);

            Assert.Throws<ChunkyardException>(
                () => repository.RetrieveValueValid(contentUri));
        }

        [Fact]
        public static void ValueValid_Returns_False_If_Not_Exists()
        {
            var repository = CreateUriRepository();
            var contentUri = new Uri("sha256://abcabcabc/");

            Assert.False(repository.ValueValid(contentUri));
        }

        [Fact]
        public static void ValueValid_Returns_False_If_Hash_Mismatch()
        {
            var repository = CreateUriRepository();
            var contentUri = new Uri("sha256://badbadbad/");
            var value = new byte[] { 0xFF };

            repository.StoreValue(contentUri, value);

            Assert.False(repository.ValueValid(contentUri));
        }

        [Fact]
        public static void ValueValid_Returns_True_If_Hash_Match()
        {
            var repository = CreateUriRepository();
            var contentUri = new Uri("sha256://a8100ae6aa1940d0b663bb31cd466142ebbdbd5187131b92d93818987832eb89/");
            var value = new byte[] { 0xFF };

            repository.StoreValue(contentUri, value);

            Assert.True(repository.ValueValid(contentUri));
        }

        [Fact]
        public static void KeepLastValues_Keeps_Last()
        {
            var repository = CreateIntRepository();
            var value = new byte[] { 0xFF };

            repository.StoreValue(0, value);
            repository.StoreValue(1, value);
            repository.StoreValue(2, value);
            repository.StoreValue(3, value);

            repository.KeepLastValues(2);

            Assert.Equal(
                new[] { 2, 3 },
                repository.ListKeys());
        }

        [Fact]
        public static void KeepLastValues_Does_Nothing_If_It_Equals_Current_Size()
        {
            var repository = CreateIntRepository();
            var value = new byte[] { 0xFF };

            repository.StoreValue(0, value);
            repository.StoreValue(1, value);

            repository.KeepLastValues(2);

            Assert.Equal(
                new[] { 0, 1 },
                repository.ListKeys());
        }

        [Fact]
        public static void KeepLastValues_Does_Nothing_If_Greater_Than_Current_Size()
        {
            var repository = CreateIntRepository();
            var value = new byte[] { 0xFF };

            repository.StoreValue(0, value);
            repository.StoreValue(1, value);

            repository.KeepLastValues(3);

            Assert.Equal(
                new[] { 0, 1 },
                repository.ListKeys());
        }

        [Fact]
        public static void KeepLastValues_Can_Empty_Log()
        {
            var repository = CreateIntRepository();
            var value = new byte[] { 0xFF };

            repository.StoreValue(0, value);
            repository.StoreValue(1, value);

            repository.KeepLastValues(0);

            Assert.Empty(repository.ListKeys());
        }

        [Fact]
        public static void Copy_Copies_Missing_Data()
        {
            var repository = CreateIntRepository();
            var otherRepository = CreateIntRepository();

            repository.StoreValue(0, new byte[] { 0x00 });
            otherRepository.StoreValue(1, new byte[] { 0xFF });

            repository.Copy(otherRepository);

            var actualKeys = otherRepository.ListKeys();
            Array.Sort(actualKeys);

            Assert.Equal(
                new[] { 0, 1 },
                actualKeys);
        }

        private static IRepository<Uri> CreateUriRepository()
        {
            return new MemoryRepository<Uri>();
        }

        private static IRepository<int> CreateIntRepository()
        {
            return new MemoryRepository<int>();
        }
    }
}
