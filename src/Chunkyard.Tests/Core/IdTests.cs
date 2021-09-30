using System;
using Chunkyard.Core;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class IdTests
    {
        [Fact]
        public static void ComputeContentUri_Creates_ContentUri_From_Content()
        {
            var content = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var expectedUri = new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");

            Assert.Equal(
                expectedUri,
                Id.ComputeContentUri(content));
        }

        [Theory]
        [InlineData("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e", true)]
        [InlineData("sha256://badbadbad", false)]
        public static void ContentUriValid_Checks_Content_Validity_Using_ContentUri(
            string hash,
            bool expectedValidity)
        {
            var content = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var contentUri = new Uri(hash);

            Assert.Equal(
                expectedValidity,
                Id.ContentUriValid(contentUri, content));
        }

        [Fact]
        public static void DeconstructContentUri_Can_Split_ContentUri()
        {
            var expectedUri = new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");

            var (hashAlgorithmName, hash) = Id.DeconstructContentUri(
                expectedUri);

            var actualUri = Id.ToContentUri(hashAlgorithmName, hash);

            Assert.Equal(expectedUri, actualUri);
        }
    }
}
