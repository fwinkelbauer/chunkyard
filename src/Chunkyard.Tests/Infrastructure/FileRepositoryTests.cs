using System;
using System.IO;
using Chunkyard.Infrastructure;
using Xunit;

namespace Chunkyard.Tests.Infrastructure
{
    public static class FileRepositoryTests
    {
        [Fact]
        public static void IntRepository_Can_Read_Write()
        {
            var repository = FileRepository.CreateIntRepository("test-repo");

            var expectedBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

            Assert.Empty(repository.ListKeys());

            repository.StoreValue(0, expectedBytes);
            repository.StoreValue(1, expectedBytes);

            Assert.Equal(
                new[] { 0, 1 },
                repository.ListKeys());

            Assert.True(repository.ValueExists(0));
            Assert.True(repository.ValueExists(1));

            Assert.Equal(expectedBytes, repository.RetrieveValue(0));
            Assert.Equal(expectedBytes, repository.RetrieveValue(1));

            repository.RemoveValue(0);
            repository.RemoveValue(1);

            Assert.Empty(repository.ListKeys());
            Assert.False(repository.ValueExists(0));
            Assert.False(repository.ValueExists(1));

            Directory.Delete(
                "test-repo",
                recursive: true);
        }

        [Fact]
        public static void UriRepository_Can_Read_Write()
        {
            var repository = FileRepository.CreateUriRepository("test-repo");

            var expectedBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var uri1 = new Uri("sha256://aa");
            var uri2 = new Uri("sha256://bb");

            Assert.Empty(repository.ListKeys());

            repository.StoreValue(uri1, expectedBytes);
            repository.StoreValue(uri2, expectedBytes);

            Assert.Equal(
                new[] { uri1, uri2 },
                repository.ListKeys());

            Assert.True(repository.ValueExists(uri1));
            Assert.True(repository.ValueExists(uri2));

            Assert.Equal(expectedBytes, repository.RetrieveValue(uri1));
            Assert.Equal(expectedBytes, repository.RetrieveValue(uri2));

            repository.RemoveValue(uri1);
            repository.RemoveValue(uri2);

            Assert.Empty(repository.ListKeys());
            Assert.False(repository.ValueExists(uri1));
            Assert.False(repository.ValueExists(uri2));

            Directory.Delete(
                "test-repo",
                recursive: true);
        }
    }
}
