using System;
using System.Security.Cryptography;
using Xunit;

namespace Chunkyard.Tests
{
    public static class IdTests
    {
        [Fact]
        public static void LogNameToUri_Creates_An_Uri_From_LogName()
        {
            var expectedUri = new Uri("log://master");

            var actualUri = Id.LogNameToUri("master");

            Assert.Equal(expectedUri, actualUri);
        }

        [Fact]
        public static void LogNameToUri_Creates_An_Uri_From_LogName_And_Position()
        {
            var expectedUri = new Uri("log://master?id=5");

            var actualUri = Id.LogNameToUri("master", 5);

            Assert.Equal(expectedUri, actualUri);
        }

        [Fact]
        public static void LogUriToParts_Can_Split_LogUri_Without_Position()
        {
            var (name, position) = Id.LogUriToParts(new Uri("log://master"));

            Assert.Equal("master", name);
            Assert.Null(position);
        }

        [Fact]
        public static void LogUriToParts_Can_Split_LogUri_With_Position()
        {
            var (name, position) = Id.LogUriToParts(new Uri("log://master?id=5"));

            Assert.Equal("master", name);
            Assert.Equal(5, position!.Value);
        }

        [Fact]
        public static void ComputeContentUri_CreatesUri_From_Content()
        {
            var expectedUri = new Uri("sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");

            var actualUri = Id.ComputeContentUri(HashAlgorithmName.SHA256, new byte[]
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
