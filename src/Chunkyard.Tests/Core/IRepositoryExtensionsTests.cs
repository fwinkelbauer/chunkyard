using System;
using System.Security.Cryptography;
using Chunkyard.Core;
using Chunkyard.Tests.Infrastructure;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class IRepositoryExtensionsTests
    {
        [Fact]
        public static void StoreValue_Detects_Already_Stored_Value()
        {
            var repository = CreateRepository();
            var hashAlgorithmName = HashAlgorithmName.SHA256;
            var content = new byte[] { 0xFF };
            var expectedContentUri = new Uri("sha256://a8100ae6aa1940d0b663bb31cd466142ebbdbd5187131b92d93818987832eb89/");

            var actualContentUri1 = repository.StoreValue(
                hashAlgorithmName,
                content,
                out var isNewValue1);

            var actualContentUri2 = repository.StoreValue(
                hashAlgorithmName,
                content,
                out var isNewValue2);

            Assert.Equal(expectedContentUri, actualContentUri1);
            Assert.Equal(expectedContentUri, actualContentUri2);
            Assert.True(isNewValue1);
            Assert.False(isNewValue2);
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
            var content = new byte[] { 0xFF };

            repository.StoreValue(contentUri, content);

            Assert.False(repository.ValueValid(contentUri));
        }

        [Fact]
        public static void ValueValid_Returns_True_If_Hash_Match()
        {
            var repository = CreateRepository();
            var contentUri = new Uri("sha256://a8100ae6aa1940d0b663bb31cd466142ebbdbd5187131b92d93818987832eb89/");
            var content = new byte[] { 0xFF };

            repository.StoreValue(contentUri, content);

            Assert.True(repository.ValueValid(contentUri));
        }

        [Fact]
        public static void KeepLatestLogPositions_Keeps_Latest()
        {
            var repository = CreateRepository();
            var content = new byte[] { 0xFF };

            repository.AppendToLog(0, content);
            repository.AppendToLog(1, content);
            repository.AppendToLog(2, content);
            repository.AppendToLog(3, content);

            repository.KeepLatestLogPositions(2);

            Assert.Equal(
                new[] { 2, 3 },
                repository.ListLogPositions());
        }

        [Fact]
        public static void KeepLatestLogPositions_Does_Nothing_If_It_Equals_Current_Size()
        {
            var repository = CreateRepository();
            var content = new byte[] { 0xFF };

            repository.AppendToLog(0, content);
            repository.AppendToLog(1, content);

            repository.KeepLatestLogPositions(2);

            Assert.Equal(
                new[] { 0, 1 },
                repository.ListLogPositions());
        }

        [Fact]
        public static void KeepLatestLogPositions_Does_Nothing_If_Greater_Than_Current_Size()
        {
            var repository = CreateRepository();
            var content = new byte[] { 0xFF };

            repository.AppendToLog(0, content);
            repository.AppendToLog(1, content);

            repository.KeepLatestLogPositions(3);

            Assert.Equal(
                new[] { 0, 1 },
                repository.ListLogPositions());
        }

        [Fact]
        public static void KeepLatestLogPositions_Can_Empty_Log()
        {
            var repository = CreateRepository();
            var content = new byte[] { 0xFF };

            repository.AppendToLog(0, content);
            repository.AppendToLog(1, content);

            repository.KeepLatestLogPositions(0);

            Assert.Empty(repository.ListLogPositions());
        }

        private static IRepository CreateRepository()
        {
            return new MemoryRepository();
        }
    }
}
