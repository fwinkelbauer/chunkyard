namespace Chunkyard.Tests.Core;

[TestClass]
public sealed class SerializeTests
{
    [TestMethod]
    public void Serialize_Can_Convert_Snapshot()
    {
        var date = DateTime.Parse("2024-06-07T12:06:43.6536137Z")
            .ToUniversalTime();

        var expected = new Snapshot(
            date,
            new[]
            {
                new BlobReference(
                    new Blob("some blob", date),
                    new[] { "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e" })
            });

        var actual = Serialize.BytesToSnapshot(
            Serialize.SnapshotToBytes(expected));

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Serialize_Can_Convert_SnapshotReference()
    {
        var expected = new SnapshotReference(
            new byte[] { 158, 128, 181, 90, 139, 201, 73, 163, 30, 55, 127, 23 },
            100000,
            new[] { "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e" });

        var actual = Serialize.BytesToSnapshotReference(
            Serialize.SnapshotReferenceToBytes(expected));

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Serialize_Respects_Serialized_Snapshot()
    {
        var date = DateTime.Parse("2024-06-07T12:06:43.6536137Z")
            .ToUniversalTime();

        var expected = new Snapshot(
            date,
            new[]
            {
                new BlobReference(
                    new Blob("some blob", date),
                    new[] { "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e" })
            });

        var json = """
            {
              "CreationTimeUtc": "2024-06-07T12:06:43.6536137Z",
              "BlobReferences": [
                {
                  "Blob": {
                    "Name": "some blob",
                    "LastWriteTimeUtc": "2024-06-07T12:06:43.6536137Z"
                  },
                  "ChunkIds": [
                    "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e"
                  ]
                }
              ]
            }
            """;

        var actual = Serialize.BytesToSnapshot(
            Encoding.UTF8.GetBytes(json));

        Assert.IsTrue(
            expected.Equals(actual),
            "Backups of previous Chunkyard versions cannot be read");
    }

    [TestMethod]
    public void Serialize_Respects_Serialized_SnapshotReference()
    {
        var expected = new SnapshotReference(
            new byte[] { 158, 128, 181, 90, 139, 201, 73, 163, 30, 55, 127, 23 },
            100000,
            new[] { "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e" });

        var json = """
            {
              "Salt": "noC1WovJSaMeN38X",
              "Iterations": 100000,
              "ChunkIds": [
                "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e"
              ]
            }
            """;

        var actual = Serialize.BytesToSnapshotReference(
            Encoding.UTF8.GetBytes(json));

        Assert.IsTrue(
            expected.Equals(actual),
            "Backups of previous Chunkyard versions cannot be read");
    }
}
