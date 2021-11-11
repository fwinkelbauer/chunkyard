namespace Chunkyard.Tests.Infrastructure
{
    public static class BlobSystemTests
    {
        [Fact]
        public static void MemoryBlobSystem_Can_Read_Write()
        {
            BlobSystem_Can_Read_Write(new MemoryBlobSystem());
        }

        [Fact]
        public static void FileBlobSystem_Can_Read_Write()
        {
            using var directory = new DisposableDirectory();
            var blobSystem = new FileBlobSystem(new[] { directory.Name });

            BlobSystem_Can_Read_Write(blobSystem);
        }

        private static void BlobSystem_Can_Read_Write(
            IBlobSystem blobSystem)
        {
            var blob = new Blob("some-blob", DateTime.UtcNow);
            var expectedBytes = new byte[] { 0x12, 0x34 };

            Assert.False(blobSystem.BlobExists(blob.Name));

            using (var writeStream = blobSystem.OpenWrite(blob))
            {
                writeStream.Write(expectedBytes);
            }

            Assert.True(blobSystem.BlobExists(blob.Name));

            Assert.Equal(
                blob,
                blobSystem.FetchMetadata(blob.Name));

            Assert.Equal(
                new[] { blob },
                blobSystem.FetchBlobs(Fuzzy.Default));

            using (var readStream = blobSystem.OpenRead(blob.Name))
            using (var memoryStream = new MemoryStream())
            {
                readStream.CopyTo(memoryStream);

                Assert.Equal(
                    expectedBytes,
                    memoryStream.ToArray());
            }
        }
    }
}
