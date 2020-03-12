using System;
using Xunit;

namespace Chunkyard.Tests
{
    public static class ValidationExtensionsTests
    {
        [Fact]
        public static void EnsureNotNull_Throws_If_Null()
        {
            string value = null!;
            var ex = Assert.Throws<ArgumentNullException>(
                () => value.EnsureNotNull(nameof(value)));

            Assert.Equal(nameof(value), ex.ParamName);
        }
    }
}
