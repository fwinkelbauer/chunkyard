using System;
using Chunkyard.Core;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class IdTests
    {
        [Fact]
        public static void ComputeContentUri_Creates_Uri_From_Content()
        {
            var content = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var expectedUri = new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");

            var actualUri = Id.ComputeContentUri(
                Id.AlgorithmSHA256,
                content);

            Assert.Equal(expectedUri, actualUri);
        }

        [Fact]
        public static void ContentUriValid_Returns_False_If_Not_Valid()
        {
            var contentUri = new Uri("sha256://badbadbad");
            var content = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

            Assert.False(Id.ContentUriValid(contentUri, content));
        }

        [Fact]
        public static void ContentUriValid_Returns_True_If_Valid()
        {
            var contentUri = new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");
            var content = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

            Assert.True(Id.ContentUriValid(contentUri, content));
        }

        [Fact]
        public static void DeconstructContentUri_Can_Split_Content_Uri()
        {
            var contentUri = new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");
            var expectedAlgorithm = Id.AlgorithmSHA256;
            var expectedHash = "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e";

            var (actualAlgorithm, actualHash) = Id.DeconstructContentUri(
                contentUri);

            Assert.Equal(expectedAlgorithm, actualAlgorithm);
            Assert.Equal(expectedHash, actualHash);
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
