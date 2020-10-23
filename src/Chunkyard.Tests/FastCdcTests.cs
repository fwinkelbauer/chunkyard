using System.IO;
using System.Linq;
using Xunit;

namespace Chunkyard.Tests
{
    public static class FastCdcTests
    {
        [Fact]
        public static void SplitIntoChunks_Sekien_16k_Chunks()
        {
            var fastCdc = new FastCdc(
                8 * 1024,
                16 * 1024,
                32 * 1024);

            using var stream = File.OpenRead("SekienAkashita.jpg");
            var chunks = fastCdc.SplitIntoChunks(stream).ToArray();

            Assert.Equal(
                new[] { 22366, 8282, 16303, 18696, 32768, 11051 },
                chunks.Select(c => c.Length));
        }

        [Fact]
        public static void SplitIntoChunks_Sekien_32k_Chunks()
        {
            var fastCdc = new FastCdc(
                16 * 1024,
                32 * 1024,
                64 * 1024);

            using var stream = File.OpenRead("SekienAkashita.jpg");
            var chunks = fastCdc.SplitIntoChunks(stream).ToArray();

            Assert.Equal(
                new[] { 32857, 16408, 60201 },
                chunks.Select(c => c.Length));
        }

        [Fact]
        public static void SplitIntoChunks_Sekien_64k_Chunks()
        {
            var fastCdc = new FastCdc(
                32 * 1024,
                64 * 1024,
                128 * 1024);

            using var stream = File.OpenRead("SekienAkashita.jpg");
            var chunks = fastCdc.SplitIntoChunks(stream).ToArray();

            Assert.Equal(
                new[] { 32857, 76609 },
                chunks.Select(c => c.Length));
        }

        [Theory]
        [InlineData(65537, 16)]
        [InlineData(65536, 16)]
        [InlineData(65535, 16)]
        [InlineData(32769, 15)]
        [InlineData(32768, 15)]
        [InlineData(32767, 15)]
        public static void Logarithm2_Test(int inputValue, int expectedValue)
        {
            Assert.Equal(expectedValue, FastCdc.Logarithm2(inputValue));
        }

        [Theory]
        [InlineData(10, 5, 2)]
        [InlineData(11, 5, 3)]
        [InlineData(10, 3, 4)]
        [InlineData(9, 3, 3)]
        [InlineData(6, 2, 3)]
        [InlineData(5, 2, 3)]
        public static void CeilDiv_Test(int x, int y, int expectedValue)
        {
            Assert.Equal(expectedValue, FastCdc.CeilDiv(x, y));
        }

        [Theory]
        [InlineData(50, 100, 50, 0)]
        [InlineData(200, 100, 50, 50)]
        [InlineData(200, 100, 40, 40)]
        public static void CenterSize_Test(
            int average,
            int minimum,
            int sourceSize,
            int expectedValue)
        {
            Assert.Equal(
                expectedValue,
                FastCdc.CenterSize(average, minimum, sourceSize));
        }

        [Theory]
        [InlineData(24, 16777215)]
        [InlineData(16, 65535)]
        [InlineData(10, 1023)]
        [InlineData(8, 255)]
        public static void Mask_Test(int bits, uint expectedValue)
        {
            Assert.Equal(expectedValue, FastCdc.Mask(bits));
        }
    }
}
