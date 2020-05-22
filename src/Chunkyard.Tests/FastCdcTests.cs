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

            Assert.Equal(6, chunks.Length);
            Assert.Equal(22366, chunks[0].Length);
            Assert.Equal(8282, chunks[1].Length);
            Assert.Equal(16303, chunks[2].Length);
            Assert.Equal(18696, chunks[3].Length);
            Assert.Equal(32768, chunks[4].Length);
            Assert.Equal(11051, chunks[5].Length);
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

            Assert.Equal(3, chunks.Length);
            Assert.Equal(32857, chunks[0].Length);
            Assert.Equal(16408, chunks[1].Length);
            Assert.Equal(60201, chunks[2].Length);
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

            Assert.Equal(2, chunks.Length);
            Assert.Equal(32857, chunks[0].Length);
            Assert.Equal(76609, chunks[1].Length);
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

        [Fact]
        public static void Logarithm2_Test_Assumptions()
        {
            Assert.True(FastCdc.Logarithm2(FastCdc.AverageMin) >= 8);
            Assert.True(FastCdc.Logarithm2(FastCdc.AverageMax) >= 28);
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
