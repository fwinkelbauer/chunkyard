using System;
using Xunit;

namespace Chunkyard.Tests
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

            var actualValue = expectedValue
                .EnsureNotNull(nameof(expectedValue));

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public static void EnsureNotNullOrEmpty_Throws_If_Null_Or_Empty(
            string? value)
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => value!.EnsureNotNullOrEmpty(nameof(value)));

            Assert.Equal(nameof(value), ex.ParamName);
        }

        [Fact]
        public static void EnsureNotNull_Returns_Non_Null_Or_Empty_Value()
        {
            var expectedValue = "hello!";

            var actualValue = expectedValue
                .EnsureNotNullOrEmpty(nameof(expectedValue));

            Assert.Equal(expectedValue, actualValue);
        }
    }
}
