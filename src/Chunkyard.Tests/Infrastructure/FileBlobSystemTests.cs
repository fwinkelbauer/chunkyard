using System;
using System.IO;
using Chunkyard.Core;
using Chunkyard.Infrastructure;
using Xunit;

namespace Chunkyard.Tests.Infrastructure
{
    public sealed class FileBlobSystemTests : IDisposable
    {
        private readonly string _directory;

        public FileBlobSystemTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                $"chunkyard-blob-system-{Path.GetRandomFileName()}");
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Fact]
        public void BlobSystem_Can_Read_Write()
        {
            var blobSystem = new FileBlobSystem(new[] { _directory });
            var blobName = "some-blob";
            var expectedBytes = new byte[] { 0x12, 0x34 };

            Assert.False(blobSystem.BlobExists(blobName));

            using (var writeStream = blobSystem.OpenWrite(blobName))
            {
                writeStream.Write(expectedBytes);
            }

            Assert.True(blobSystem.BlobExists(blobName));
            Assert.Equal(
                blobName,
                blobSystem.FetchMetadata(blobName).Name);

            using (var readStream = blobSystem.OpenRead(blobName))
            {
                using var memoryStream = new MemoryStream();

                readStream.CopyTo(memoryStream);

                Assert.Equal(
                    expectedBytes,
                    memoryStream.ToArray());
            }

            var blob = new Blob(blobName, DateTime.UtcNow);

            Assert.NotEqual(
                blob,
                blobSystem.FetchMetadata(blobName));

            blobSystem.UpdateMetadata(blob);

            Assert.Equal(
                blob,
                blobSystem.FetchMetadata(blobName));

            Assert.Equal(
                new[] { blob },
                blobSystem.FetchBlobs(Fuzzy.ExcludeNothing));
        }
    }
}
