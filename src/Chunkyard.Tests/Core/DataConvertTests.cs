namespace Chunkyard.Tests.Core;

public static class DataConvertTests
{
    [Fact]
    public static void DataConvert_Converts_Objects_To_Bytes_And_Back()
    {
        var blob = new Blob("some blob", DateTime.UtcNow);

        var bytes = DataConvert.ObjectToBytes(blob);

        Assert.Equal(
            blob,
            DataConvert.BytesToObject<Blob>(bytes));
    }

    [Fact]
    public static void DataConvert_Converts_Text_To_Bytes_And_Back()
    {
        var text = "Hello World!";

        var bytes = DataConvert.TextToBytes(text);

        Assert.Equal(
            text,
            DataConvert.BytesToText(bytes));
    }

    [Fact]
    public static void DataConvert_Can_Read_Serialized_Snapshot()
    {
        var serializedSnapshot = new
        {
            SnapshotId = 15,
            CreationTimeUtc = "2020-05-07T18:33:00Z",
            BlobReferences = new[]
            {
                new
                {
                    Name = "some blob",
                    LastWriteTimeUtc = "2020-05-07T18:33:00Z",
                    Nonce = "ESIzRA==",
                    ContentUris = new[]
                    {
                        "sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e"
                    }
                }
            }
        };

        var currentSnapshot = new Snapshot(
            15,
            new DateTime(2020, 05, 07, 18, 33, 0, DateTimeKind.Utc),
            new[]
            {
                new BlobReference(
                    "some blob",
                    new DateTime(2020, 05, 07, 18, 33, 0, DateTimeKind.Utc),
                    new byte[] { 0x11, 0x22, 0x33, 0x44 },
                    new[]
                    {
                        new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e")
                    })
            });

        var expectedJson = DataConvert.BytesToText(
            DataConvert.ObjectToBytes(serializedSnapshot));

        var actualJson = DataConvert.BytesToText(
            DataConvert.ObjectToBytes(currentSnapshot));

        Assert.True(
            expectedJson.Equals(actualJson),
            "Broke persisted repository format");
    }

    [Fact]
    public static void DataConvert_Can_Read_Serialized_SnapshotReference()
    {
        var serializedReference = new
        {
            Salt = "ESIzRA==",
            Iterations = 1000,
            ContentUris = new[]
            {
                "sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e"
            }
        };

        var currentReference = new SnapshotReference(
            new byte[] { 0x11, 0x22, 0x33, 0x44 },
            1000,
            new[]
            {
                new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e")
            });

        var expectedJson = DataConvert.BytesToText(
            DataConvert.ObjectToBytes(serializedReference));

        var actualJson = DataConvert.BytesToText(
            DataConvert.ObjectToBytes(currentReference));

        Assert.True(
            expectedJson.Equals(actualJson),
            "Broke persisted repository format");
    }
}
