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
            var repository = CreateRepository();
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
        public static void RetrieValueValid_Retrieves_Valid_Value()
        {
            var repository = CreateRepository();
            var expectedValue = new byte[] { 0xFF };

            var contentUri = repository.StoreValue(HashAlgorithmName.SHA256, expectedValue);

            Assert.Equal(
                expectedValue,
                repository.RetrieveValueValid(contentUri));
        }

        [Fact]
        public static void RetrieValueValid_Throws_If_Value_Invalid()
        {
            var repository = CreateRepository();
            var contentUri = new Uri("sha256://badbadbad");
            var value = new byte[] { 0xFF };

            repository.StoreValue(contentUri, value);

            Assert.Throws<ChunkyardException>(
                () => repository.RetrieveValueValid(contentUri));
        }

        [Fact]
        public static void ValueValid_Returns_False_If_Not_Exists()
        {
            var repository = CreateRepository();
            var contentUri = new Uri("sha256://abcabcabc/");

            Assert.False(repository.ValueValid(contentUri));
        }

        [Fact]
        public static void ValueValid_Returns_False_If_Hash_Mismatch()
        {
            var repository = CreateRepository();
            var contentUri = new Uri("sha256://badbadbad/");
            var value = new byte[] { 0xFF };

            repository.StoreValue(contentUri, value);

            Assert.False(repository.ValueValid(contentUri));
        }

        [Fact]
        public static void ValueValid_Returns_True_If_Hash_Match()
        {
            var repository = CreateRepository();
            var contentUri = new Uri("sha256://a8100ae6aa1940d0b663bb31cd466142ebbdbd5187131b92d93818987832eb89/");
            var value = new byte[] { 0xFF };

            repository.StoreValue(contentUri, value);

            Assert.True(repository.ValueValid(contentUri));
        }

        [Fact]
        public static void KeepLatestLogPositions_Keeps_Latest()
        {
            var repository = CreateRepository();
            var value = new byte[] { 0xFF };

            repository.AppendToLog(0, value);
            repository.AppendToLog(1, value);
            repository.AppendToLog(2, value);
            repository.AppendToLog(3, value);

            repository.KeepLatestLogPositions(2);

            Assert.Equal(
                new[] { 2, 3 },
                repository.ListLogPositions());
        }

        [Fact]
        public static void KeepLatestLogPositions_Does_Nothing_If_It_Equals_Current_Size()
        {
            var repository = CreateRepository();
            var value = new byte[] { 0xFF };

            repository.AppendToLog(0, value);
            repository.AppendToLog(1, value);

            repository.KeepLatestLogPositions(2);

            Assert.Equal(
                new[] { 0, 1 },
                repository.ListLogPositions());
        }

        [Fact]
        public static void KeepLatestLogPositions_Does_Nothing_If_Greater_Than_Current_Size()
        {
            var repository = CreateRepository();
            var value = new byte[] { 0xFF };

            repository.AppendToLog(0, value);
            repository.AppendToLog(1, value);

            repository.KeepLatestLogPositions(3);

            Assert.Equal(
                new[] { 0, 1 },
                repository.ListLogPositions());
        }

        [Fact]
        public static void KeepLatestLogPositions_Can_Empty_Log()
        {
            var repository = CreateRepository();
            var value = new byte[] { 0xFF };

            repository.AppendToLog(0, value);
            repository.AppendToLog(1, value);

            repository.KeepLatestLogPositions(0);

            Assert.Empty(repository.ListLogPositions());
        }

        private static IRepository CreateRepository()
        {
            return new MemoryRepository();
        }
    }
}
