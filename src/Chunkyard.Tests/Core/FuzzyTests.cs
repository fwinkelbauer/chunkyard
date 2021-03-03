﻿using Chunkyard.Core;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class FuzzyTests
    {
        [Fact]
        public static void EmptyPattern_Matches_Everything()
        {
            var fuzzy = new Fuzzy("");

            Assert.True(fuzzy.IsMatch("some text!"));
        }

        [Fact]
        public static void Spaces_Are_Treated_As_Wildcards()
        {
            var fuzzy = new Fuzzy("He ld");

            Assert.True(fuzzy.IsMatch("Hello World!"));
            Assert.True(fuzzy.IsMatch("Held"));
            Assert.False(fuzzy.IsMatch("Goodbye World!"));
        }
    }
}