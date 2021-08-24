using System;
using Chunkyard.Core;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class ValidationExtensionsTests
    {
        [Fact]
        public static void EnsureNotNull_Throws_If_Null()
        {
            string? value = null;

            var ex = Assert.Throws<ArgumentNullException>(
                () => value.EnsureNotNull(nameof(value)));

            Assert.Equal(nameof(value), ex.ParamName);
        }

        [Fact]
        public static void EnsureNotNull_Returns_Non_Null_Value()
        {
            var expectedValue = "hello";

            var actualValue = expectedValue.EnsureNotNull(
                nameof(expectedValue));

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(11)]
        public static void EnsureBetween_Throws_If_Value_Is_Not_In_Range(
            int value)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => value.EnsureBetween(0, 10, nameof(value)));

            Assert.Equal(nameof(value), ex.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(10)]
        public static void EnsureBetween_Returns_Value_If_In_Range(int value)
        {
            var actualValue = value.EnsureBetween(
                0,
                10,
                nameof(value));

            Assert.Equal(value, actualValue);
        }
    }
}
