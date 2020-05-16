using Xunit;

namespace Chunkyard.Tests
{
    public static class FastCdcTests
    {
        [Theory]
        [InlineData(65537, 16)]
        [InlineData(65536, 16)]
        [InlineData(65535, 16)]
        [InlineData(32769, 15)]
        [InlineData(32768, 15)]
        [InlineData(32767, 15)]
        public static void Test_Logarithm2(int inputValue, int expectedValue)
        {
            Assert.Equal(expectedValue, FastCdc.Logarithm2(inputValue));
        }

        [Fact]
        public static void Test_Logarithm2_Assumptions()
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
        public static void Test_CeilDiv(int x, int y, int expectedValue)
        {
            Assert.Equal(expectedValue, FastCdc.CeilDiv(x, y));
        }

        [Theory]
        [InlineData(50, 100, 50, 0)]
        [InlineData(200, 100, 50, 50)]
        [InlineData(200, 100, 40, 40)]
        public static void Test_CenterSize(
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
        public static void Test_Mask(int bits, uint expectedValue)
        {
            Assert.Equal(expectedValue, FastCdc.Mask(bits));
        }
    }
}
