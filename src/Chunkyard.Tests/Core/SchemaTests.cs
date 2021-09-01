using System;
using Chunkyard.Core;
using Xunit;

namespace Chunkyard.Tests.Core
{
    /// <summary>
    // Breaking any of these tests indicate a breaking change in the persisted
    // repository format.
    /// </summary>
    public static class SchemaTests
    {
        [Fact]
        public static void Can_Read_Serialized_Snapshot()
        {
            var serialized = @"
{
  ""SnapshotId"": 15,
  ""CreationTimeUtc"": ""2020-05-07T18:33:00Z"",
  ""BlobReferences"": [
    {
      ""Name"": ""some blob"",
      ""LastWriteTimeUtc"": ""2020-05-07T18:33:00Z"",
      ""Nonce"": ""ESIzRA=="",
      ""ContentUris"": [
        ""sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e""
      ]
    }
  ]
}";
            var date = new DateTime(2020, 05, 07, 18, 33, 0, DateTimeKind.Utc);
            var expectedSnapshot = new Snapshot(
                15,
                date,
                new[]
                {
                    new BlobReference(
                        "some blob",
                        date,
                        new byte[] { 0x11, 0x22, 0x33, 0x44 },
                        new[]
                        {
                            new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e")
                        })
                });

            var actualSnapshot = DataConvert.BytesToObject<Snapshot>(
                DataConvert.TextToBytes(serialized));

            Assert.Equal(expectedSnapshot, actualSnapshot);
        }

        [Fact]
        public static void Can_Read_Serialized_SnapshotReference()
        {
            var serialized = @"
{
  ""Salt"": ""ESIzRA=="",
  ""Iterations"": 1000,
  ""ContentUris"": [
    ""sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e""
  ]
}";
            var expectedReference = new SnapshotReference(
                new byte[] { 0x11, 0x22, 0x33, 0x44 },
                1000,
                new[]
                {
                    new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e")
                });

            var actualReference = DataConvert.BytesToObject<SnapshotReference>(
                DataConvert.TextToBytes(serialized));

            Assert.Equal(expectedReference, actualReference);
        }
    }
}
