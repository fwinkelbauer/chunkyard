using System;
using System.IO;
using System.Linq;
using Chunkyard.Infrastructure;
using Xunit;

namespace Chunkyard.Tests.Infrastructure
{
    public sealed class FileRepositoryTests : IDisposable
    {
        private readonly string _directory;

        public FileRepositoryTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                $"chunkyard-test-repo-{Path.GetRandomFileName()}");
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Fact]
        public void IntRepository_Can_Read_Write()
        {
            var repository = FileRepository.CreateIntRepository(_directory);

            var expectedBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

            Assert.Empty(repository.ListKeys());

            repository.StoreValue(0, expectedBytes);
            repository.StoreValue(1, expectedBytes);

            Assert.Equal(
                new[] { 0, 1 },
                repository.ListKeys().OrderBy(i => i));

            Assert.True(repository.ValueExists(0));
            Assert.True(repository.ValueExists(1));

            Assert.Equal(expectedBytes, repository.RetrieveValue(0));
            Assert.Equal(expectedBytes, repository.RetrieveValue(1));

            repository.RemoveValue(0);
            repository.RemoveValue(1);

            Assert.Empty(repository.ListKeys());
            Assert.False(repository.ValueExists(0));
            Assert.False(repository.ValueExists(1));
        }

        [Fact]
        public void UriRepository_Can_Read_Write()
        {
            var repository = FileRepository.CreateUriRepository(_directory);

            var expectedBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var uri1 = new Uri("sha256://aa");
            var uri2 = new Uri("sha256://bb");

            Assert.Empty(repository.ListKeys());

            repository.StoreValue(uri1, expectedBytes);
            repository.StoreValue(uri2, expectedBytes);

            Assert.Equal(
                new[] { uri1, uri2 },
                repository.ListKeys().OrderBy(u => u.AbsoluteUri));

            Assert.True(repository.ValueExists(uri1));
            Assert.True(repository.ValueExists(uri2));

            Assert.Equal(expectedBytes, repository.RetrieveValue(uri1));
            Assert.Equal(expectedBytes, repository.RetrieveValue(uri2));

            repository.RemoveValue(uri1);
            repository.RemoveValue(uri2);

            Assert.Empty(repository.ListKeys());
            Assert.False(repository.ValueExists(uri1));
            Assert.False(repository.ValueExists(uri2));
        }
    }
}
