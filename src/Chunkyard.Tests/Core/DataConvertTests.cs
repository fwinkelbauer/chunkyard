namespace Chunkyard.Tests.Core;

public static class DataConvertTests
{
    [Fact]
    public static void DataConvert_Can_Convert_Snapshot()
    {
        var expected = new Snapshot(
            DateTime.UtcNow,
            new[]
            {
                new BlobReference(
                    new Blob("some blob", DateTime.UtcNow),
                    Crypto.GenerateNonce(),
                    new[] { "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e" })
            });

        var actual = DataConvert.BytesToSnapshot(
            DataConvert.SnapshotToBytes(expected));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void DataConvert_Can_Convert_SnapshotReference()
    {
        var expected = new SnapshotReference(
            Crypto.GenerateNonce(),
            1000,
            new[] { "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e" });

        var actual = DataConvert.BytesToSnapshotReference(
            DataConvert.SnapshotReferenceToBytes(expected));

        Assert.Equal(expected, actual);
    }
}
