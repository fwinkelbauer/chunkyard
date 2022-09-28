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

        var blobSystem = new FileBlobSystem(
            new[] { directory.Name },
            Fuzzy.Default);

        BlobSystem_Can_Read_Write(blobSystem);
    }

    [Theory]
    [InlineData("../directory-traversal")]
    [InlineData("excluded-blob")]
    public static void FileBlobSystem_Prevents_Accessing_Invalid_Blobs(
        string invalidBlobName)
    {
        using var directory = new DisposableDirectory();

        var blobSystem = new FileBlobSystem(
            new[] { directory.Name },
            new Fuzzy(new[] { "excluded-blob" }));

        var invalidBlob = Some.Blob(invalidBlobName);

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
    }

    [Fact]
    public static void FileBlobSystem_OpenWrite_Overwrites_Previous_Content()
    {
        using var directory = new DisposableDirectory();

        var blobSystem = new FileBlobSystem(
            new[] { directory.Name },
            Fuzzy.Default);

        var blob = Some.Blob("some blob");

        using (var writeStream = blobSystem.OpenWrite(blob))
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

    [Fact]
    public static void FileBlobSystem_Finds_Parent_Given_More_Paths()
    {
        using var directory = new DisposableDirectory();

        var subDirectories = new[]
        {
            Path.Combine(directory.Name, "sub1"),
            Path.Combine(directory.Name, "sub2")
        };

        foreach (var subDirectory in subDirectories)
        {
            Directory.CreateDirectory(subDirectory);

            File.WriteAllText(
                Path.Combine(subDirectory, "file.txt"),
                "some text");
        }

        var blobSystem = new FileBlobSystem(subDirectories, Fuzzy.Default);

        Assert.Equal(
            new[] { "sub1/file.txt", "sub2/file.txt" },
            blobSystem.ListBlobs().Select(b => b.Name));
    }

    private static void BlobSystem_Can_Read_Write(
        IBlobSystem blobSystem)
    {
        var blob = Some.Blob("some blob");
        var expectedBytes = new byte[] { 0x12, 0x34 };

        Assert.False(blobSystem.BlobExists(blob.Name));

        using (var writeStream = blobSystem.OpenWrite(blob))
        {
            writeStream.Write(expectedBytes);
        }

        Assert.True(blobSystem.BlobExists(blob.Name));

        Assert.Equal(
            blob,
            blobSystem.GetBlob(blob.Name));

        Assert.Equal(
            new[] { blob },
            blobSystem.ListBlobs());

        using (var readStream = blobSystem.OpenRead(blob.Name))
        using (var memoryStream = new MemoryStream())
        {
            readStream.CopyTo(memoryStream);

            Assert.Equal(
                expectedBytes,
                memoryStream.ToArray());
        }

        blobSystem.RemoveBlob(blob.Name);

        Assert.Empty(blobSystem.ListBlobs());
    }
}
