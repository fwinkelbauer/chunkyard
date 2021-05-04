using System;
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
            var hashAlgorithmName = Id.AlgorithmSHA256;
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

            var contentUri = repository.StoreValue(
                Id.AlgorithmSHA256,
                expectedValue);

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
