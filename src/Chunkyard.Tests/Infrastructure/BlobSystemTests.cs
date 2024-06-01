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

        var blobSystem = new FileBlobSystem(subDirectories);

        Assert.Equal(
            new[] { "sub1/file.txt", "sub2/file.txt" },
            blobSystem.ListBlobs().Select(b => b.Name));
    }

    public void Dispose()
    {
        _tempDirectory?.Dispose();
        _tempDirectory = null;
    }

    private static FileBlobSystem CreateBlobSystem()
    {
        _tempDirectory = new();

        return new FileBlobSystem(_tempDirectory.Name);
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

        Assert.Equal(
            expectedBytes,
            StreamUtils.AsBytes(() => BlobSystem.OpenRead(blob.Name)));
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

        Assert.Equal(
            new byte[] { 0x11 },
            StreamUtils.AsBytes(() => BlobSystem.OpenRead(blob.Name)));
    }
}
