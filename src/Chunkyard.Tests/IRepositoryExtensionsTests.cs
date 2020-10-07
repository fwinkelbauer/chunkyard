﻿using System;
using Xunit;

namespace Chunkyard.Tests
{
    public static class IRepositoryExtensionsTests
    {
        [Fact]
        public static void UriValid_Returns_False_If_Null()
        {
            var repository = new MemoryRepository();

            Assert.False(repository.UriValid(null!));
        }

        [Fact]
        public static void UriValid_Returns_False_If_Not_Exists()
        {
            var repository = new MemoryRepository();
            var contentUri = new Uri("sha256://abcabcabc/");

            Assert.False(repository.UriValid(contentUri));
        }

        [Fact]
        public static void UriValid_Returns_False_If_Hash_Mismatch()
        {
            var repository = new MemoryRepository();
            var contentUri = new Uri("sha256://badbadbad/");
            var content = new byte[] { 0xFF };

            repository.StoreValue(contentUri, content);

            Assert.False(repository.UriValid(contentUri));
        }

        [Fact]
        public static void UriValid_Returns_True_If_Hash_Match()
        {
            var repository = new MemoryRepository();
            var contentUri = new Uri("sha256://a8100ae6aa1940d0b663bb31cd466142ebbdbd5187131b92d93818987832eb89/");
            var content = new byte[] { 0xFF };

            repository.StoreValue(contentUri, content);

            Assert.True(repository.UriValid(contentUri));
        }

        [Fact]
        public static void KeepLatestLogPositions_Keeps_Latest()
        {
            var repository = new MemoryRepository();
            var content = new byte[] { 0xFF };

            repository.AppendToLog(content, 0);
            repository.AppendToLog(content, 1);
            repository.AppendToLog(content, 2);
            repository.AppendToLog(content, 3);

            repository.KeepLatestLogPositions(2);

            Assert.Equal(
                new[] { 2, 3 },
                repository.ListLogPositions());
        }

        [Fact]
        public static void KeepLatestLogPositions_Does_Nothing_If_Inside()
        {
            var repository = new MemoryRepository();
            var content = new byte[] { 0xFF };

            repository.AppendToLog(content, 0);
            repository.AppendToLog(content, 1);

            repository.KeepLatestLogPositions(2);

            Assert.Equal(
                new[] { 0, 1 },
                repository.ListLogPositions());
        }

        [Fact]
        public static void KeepLatestLogPositions_Does_Nothing_If_Over()
        {
            var repository = new MemoryRepository();
            var content = new byte[] { 0xFF };

            repository.AppendToLog(content, 0);
            repository.AppendToLog(content, 1);

            repository.KeepLatestLogPositions(3);

            Assert.Equal(
                new[] { 0, 1 },
                repository.ListLogPositions());
        }

        [Fact]
        public static void KeepLatestLogPositions_Can_Empty_Log()
        {
            var repository = new MemoryRepository();
            var content = new byte[] { 0xFF };

            repository.AppendToLog(content, 0);
            repository.AppendToLog(content, 1);

            repository.KeepLatestLogPositions(0);

            Assert.Empty(repository.ListLogPositions());
        }
    }
}