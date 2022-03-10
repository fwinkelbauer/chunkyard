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
            CreationTimeUtc = "2020-05-07T18:33:00Z",
            BlobReferences = new[]
            {
                new
                {
                    Blob = new
                    {
                        Name = "some blob",
                        LastWriteTimeUtc = "2020-05-07T18:33:00Z"
                    },
                    Nonce = "ESIzRA==",
                    ChunkIds = new[]
                    {
                        "sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e"
                    }
                }
            }
        };

        var currentSnapshot = new Snapshot(
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

        var expected = DataConvert.BytesToText(
            DataConvert.ObjectToBytes(serializedSnapshot));

        var actual = DataConvert.BytesToText(
            DataConvert.ObjectToBytes(currentSnapshot));

        Assert.True(
            expected.Equals(actual),
            "Broke persisted repository format");
    }

    [Fact]
    public static void DataConvert_Can_Read_Serialized_SnapshotReference()
    {
        var serializedReference = new
        {
            SchemaVersion = 1,
            Salt = "ESIzRA==",
            Iterations = 1000,
            ChunkIds = new[]
            {
                "sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e"
            }
        };

        var currentReference = new SnapshotReference(
            SnapshotStore.SchemaVersion,
            new byte[] { 0x11, 0x22, 0x33, 0x44 },
            1000,
            new[]
            {
                new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e")
            });

        var expected = DataConvert.BytesToText(
            DataConvert.ObjectToBytes(serializedReference));

        var actual = DataConvert.BytesToText(
            DataConvert.ObjectToBytes(currentReference));

        Assert.True(
            expected.Equals(actual),
            "Broke persisted repository format");
    }

    [Fact]
    public static void BytesToVersionedObject_Returns_Supported_Object()
    {
        var expected = new Versioned(1);

        var actual = DataConvert.BytesToVersionedObject<Versioned>(
            DataConvert.ObjectToBytes(expected),
            1);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void BytesToVersionedObject_Throws_If_Unsupported_Object()
    {
        var obj = new Versioned(1);

        Assert.Throws<NotSupportedException>(
            () => DataConvert.BytesToVersionedObject<Versioned>(
                DataConvert.ObjectToBytes(obj),
                2));
    }
}
