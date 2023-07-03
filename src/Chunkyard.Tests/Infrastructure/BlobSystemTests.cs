namespace Chunkyard.Tests.Infrastructure;

public sealed class MemoryBlobSystemTests : BlobSystemTests
{
    public MemoryBlobSystemTests()
        : base(new MemoryBlobSystem())
    {
    }
}

public sealed class FileBlobSystemTests
    : BlobSystemTests, IDisposable
{
    private static DisposableDirectory? _tempDirectory;

    public FileBlobSystemTests()
        : base(CreateBlobSystem())
    {
    }

    [Fact]
    public static void Constructor_Finds_Parent_Given_More_Paths()
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

    [Theory]
    [InlineData("../directory-traversal")]
    [InlineData("excluded-blob")]
    public static void Methods_Prevent_Accessing_Invalid_Blobs(
        string invalidBlobName)
    {
        using var directory = new DisposableDirectory();

        var blobSystem = new FileBlobSystem(
            new[] { directory.Name },
            new Fuzzy("!excluded-blob"));

        var invalidBlob = Some.Blob(invalidBlobName);

        Assert.Throws<ArgumentException>(
            () => blobSystem.BlobExists(invalidBlobName));

        Assert.Throws<ArgumentException>(
            () => blobSystem.GetBlob(invalidBlobName));

        Assert.Throws<ArgumentException>(
            () => blobSystem.OpenRead(invalidBlobName));

        Assert.Throws<ArgumentException>(
            () => blobSystem.OpenWrite(invalidBlob));
    }

    public void Dispose()
    {
        _tempDirectory?.Dispose();
        _tempDirectory = null;
    }

    private static IBlobSystem CreateBlobSystem()
    {
        _tempDirectory = new DisposableDirectory();

        return new FileBlobSystem(
            new[] { _tempDirectory.Name },
            Fuzzy.Default);
    }
}

public abstract class BlobSystemTests
{
    protected BlobSystemTests(IBlobSystem blobSystem)
    {
        BlobSystem = blobSystem;
    }

    protected IBlobSystem BlobSystem { get; }

    [Fact]
    public void BlobSystem_Can_Read_Write()
    {
        var blob = Some.Blob("some blob");
        var expectedBytes = new byte[] { 0x12, 0x34 };

        Assert.False(BlobSystem.BlobExists(blob.Name));

        using (var writeStream = BlobSystem.OpenWrite(blob))
        {
            writeStream.Write(expectedBytes);
        }

        Assert.True(BlobSystem.BlobExists(blob.Name));

        Assert.Equal(
            blob,
            BlobSystem.GetBlob(blob.Name));

        Assert.Equal(
            new[] { blob },
            BlobSystem.ListBlobs());

        using var readStream = BlobSystem.OpenRead(blob.Name);
        using var memoryStream = new MemoryStream();

        readStream.CopyTo(memoryStream);

        Assert.Equal(
            expectedBytes,
            memoryStream.ToArray());
    }

    [Fact]
    public void OpenWrite_Overwrites_Previous_Content()
    {
        var blob = Some.Blob("some blob");

        using (var writeStream = BlobSystem.OpenWrite(blob))
        {
            writeStream.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
        }

        using (var writeStream = BlobSystem.OpenWrite(blob))
        {
            writeStream.Write(new byte[] { 0x11 });
        }

        using var readStream = BlobSystem.OpenRead(blob.Name);
        using var memoryStream = new MemoryStream();

        readStream.CopyTo(memoryStream);

        Assert.Equal(
            new byte[] { 0x11 },
            memoryStream.ToArray());
    }
}
