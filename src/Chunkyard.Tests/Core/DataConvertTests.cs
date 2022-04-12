namespace Chunkyard.Tests.Core;

public static class DataConvertTests
{
    [Fact]
    public static void DataConvert_Can_Convert_Snapshot()
    {
        var expected = new Snapshot(
            new DateTime(2020, 05, 07, 18, 33, 0, DateTimeKind.Utc),
            new[]
            {
                new BlobReference(
                    new Blob(
                        "some blob",
                        new DateTime(2020, 05, 07, 18, 33, 0, DateTimeKind.Utc)),
                    new byte[] { 0x11, 0x22, 0x33, 0x44 },
                    new[]
                    {
                        new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e")
                    })
            });

        var actual = DataConvert.BytesToSnapshot(
            DataConvert.SnapshotToBytes(expected));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void DataConvert_Can_Convert_SnapshotReference()
    {
        var expected = new SnapshotReference(
            new byte[] { 0x11, 0x22, 0x33, 0x44 },
            1000,
            new[]
            {
                new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e")
            });

        var actual = DataConvert.BytesToSnapshotReference(
            DataConvert.SnapshotReferenceToBytes(expected));

        Assert.Equal(expected, actual);
    }
}
