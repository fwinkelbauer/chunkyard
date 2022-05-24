namespace Chunkyard.Tests.Infrastructure;

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

    [Fact]
    public static void FileBlobSystem_Prevents_Directory_Traversal_Attack()
    {
        using var directory = new DisposableDirectory();
        var blobSystem = new FileBlobSystem(new[] { directory.Name });

        var invalidBlobName = "../some-file";
        var invalidBlob = new Blob(
            invalidBlobName,
            Some.UtcDate);

        Assert.Throws<ArgumentException>(
            () => blobSystem.BlobExists(invalidBlobName));

        Assert.Throws<ArgumentException>(
            () => blobSystem.RemoveBlob(invalidBlobName));

        Assert.Throws<ArgumentException>(
            () => blobSystem.GetBlob(invalidBlobName));

        Assert.Throws<ArgumentException>(
            () => blobSystem.OpenRead(invalidBlobName));

        Assert.Throws<ArgumentException>(
            () => blobSystem.OpenWrite(invalidBlob));

        Assert.Throws<ArgumentException>(
            () => blobSystem.NewWrite(invalidBlob));
    }

    [Fact]
    public static void FileblobSystem_OpenWrite_Overwrites_Previous_Content()
    {
        using var directory = new DisposableDirectory();
        var blobSystem = new FileBlobSystem(new[] { directory.Name });
        var blob = Some.Blob;

        using (var writeStream = blobSystem.NewWrite(blob))
        {
            writeStream.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
        }

        using (var writeStream = blobSystem.OpenWrite(blob))
        {
            writeStream.Write(new byte[] { 0x11 });
        }

        using (var readStream = blobSystem.OpenRead(blob.Name))
        using (var memoryStream = new MemoryStream())
        {
            readStream.CopyTo(memoryStream);

            Assert.Equal(
                new byte[] { 0x11 },
                memoryStream.ToArray());
        }
    }

    private static void BlobSystem_Can_Read_Write(
        IBlobSystem blobSystem)
    {
        var blob = Some.Blob;
        var expectedBytes = new byte[] { 0x12, 0x34 };

        Assert.False(blobSystem.BlobExists(blob.Name));

        using (var writeStream = blobSystem.NewWrite(blob))
        {
            writeStream.Write(expectedBytes);
        }

        Assert.True(blobSystem.BlobExists(blob.Name));

        Assert.Equal(
            blob,
            blobSystem.GetBlob(blob.Name));

        Assert.Equal(
            new[] { blob },
            blobSystem.ListBlobs(Fuzzy.Default));

        using (var readStream = blobSystem.OpenRead(blob.Name))
        using (var memoryStream = new MemoryStream())
        {
            readStream.CopyTo(memoryStream);

            Assert.Equal(
                expectedBytes,
                memoryStream.ToArray());
        }

        blobSystem.RemoveBlob(blob.Name);

        Assert.Empty(blobSystem.ListBlobs(Fuzzy.Default));
    }
}
