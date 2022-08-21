namespace Chunkyard.Tests.Core;

public static class SerializeTests
{
    [Fact]
    public static void Serialize_Can_Convert_Snapshot()
    {
        var expected = new Snapshot(
            Some.DateUtc,
            new[]
            {
                new BlobReference(
                    Some.Blob("some blob"),
                    new[] { "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e" })
            });

        var actual = Serialize.BytesToSnapshot(
            Serialize.SnapshotToBytes(expected));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void Serialize_Can_Convert_LogReference()
    {
        var expected = new LogReference(
            Crypto.GenerateSalt(),
            Crypto.DefaultIterations,
            new[] { "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e" });

        var actual = Serialize.BytesToLogReference(
            Serialize.LogReferenceToBytes(expected));

        Assert.Equal(expected, actual);
    }
}
