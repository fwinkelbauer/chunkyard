using System;
using System.Security.Cryptography;
using Xunit;

namespace Chunkyard.Tests
{
    public static class IdTests
    {
        [Fact]
        public static void ComputeContentUri_CreatesUri_From_Content()
        {
            var expectedUri = new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");

            var actualUri = Id.ComputeContentUri(
                HashAlgorithmName.SHA256,
                new byte[]
                {
                    0xFF,
                    0xFF,
                    0xFF,
                    0xFF
                });

            Assert.Equal(expectedUri, actualUri);
        }

        [Fact]
        public static void FromContentUri_Can_Split_ContentUri()
        {
            var contentUri = new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");

            var actualAlgorithm = Id.AlgorithmFromContentUri(contentUri);
            var actualHash = Id.HashFromContentUri(contentUri);

            Assert.Equal(HashAlgorithmName.SHA256, actualAlgorithm);
            Assert.Equal("ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e", actualHash);
        }

        [Fact]
        public static void ToContentUri_Creates_ContentUri()
        {
            var expectedUri = new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");

            var actualUri = Id.ToContentUri(HashAlgorithmName.SHA256, "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");

            Assert.Equal(expectedUri, actualUri);
        }
    }
}
