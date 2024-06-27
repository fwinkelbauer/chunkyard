namespace Chunkyard.Tests.Infrastructure;

public sealed class MemoryBlobSystemTests : BlobSystemTests
{
    public MemoryBlobSystemTests()
        : base(new MemoryBlobSystem())
    {
    }
}

public sealed class FileBlobSystemTests : BlobSystemTests
{
    public FileBlobSystemTests()
        : base(new FileBlobSystem(Some.Directory()))
    {
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
        var expected = new byte[] { 0x11 };

        using (var writeStream = BlobSystem.OpenWrite(blob))
        {
            writeStream.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
        }

        using (var writeStream = BlobSystem.OpenWrite(blob))
        {
            writeStream.Write(expected);
        }

        Assert.Equal(
            expected,
            StreamUtils.AsBytes(() => BlobSystem.OpenRead(blob.Name)));
    }

    [Fact]
    public void ListBlobs_Sorts_By_Name()
    {
        var blob1 = Some.Blob("z");
        var blob2 = Some.Blob("a");

        using (var writeStream = BlobSystem.OpenWrite(blob1))
        {
            writeStream.Write(new byte[] { 0xFF });
        }

        using (var writeStream = BlobSystem.OpenWrite(blob2))
        {
            writeStream.Write(new byte[] { 0xFF });
        }

        Assert.Equal(
            new[] { blob2, blob1 },
            BlobSystem.ListBlobs());
    }
}
