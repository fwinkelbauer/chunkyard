using System;
using System.Security.Cryptography;
using Chunkyard.Core;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class IdTests
    {
        [Fact]
        public static void ComputeContentUri_Creates_Uri_From_Content()
        {
            var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var expectedUri = new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");

            var actualUri = Id.ComputeContentUri(
                HashAlgorithmName.SHA256,
                bytes);

            Assert.Equal(expectedUri, actualUri);
        }

        [Fact]
        public static void FromContentUri_Can_Split_Content_Uri()
        {
            var contentUri = new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");
            var expectedAlgorithm = HashAlgorithmName.SHA256;
            var expectedHash = "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e";

            var actualAlgorithm = Id.AlgorithmFromContentUri(contentUri);
            var actualHash = Id.HashFromContentUri(contentUri);

            Assert.Equal(expectedAlgorithm, actualAlgorithm);
            Assert.Equal(expectedHash, actualHash);
        }

        [Fact]
        public static void ComputeHash_Creates_Hash_From_Bytes()
        {
            var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var expectedhash = "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e";

            var actualHash = Id.ComputeHash(
                HashAlgorithmName.SHA256,
                bytes);

            Assert.Equal(expectedhash, actualHash);
        }

        [Fact]
        public static void ToContentUri_Creates_Uri_From_Algorithm_And_Hash()
        {
            var hashAlgorithmName = "sha256";
            var hash = "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e";
            var expectedUri = new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");

            var actualUri = Id.ToContentUri(hashAlgorithmName, hash);

            Assert.Equal(expectedUri, actualUri);
        }
    }
}
