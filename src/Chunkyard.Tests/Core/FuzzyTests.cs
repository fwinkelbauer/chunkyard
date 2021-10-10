using System;
using Chunkyard.Core;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class FuzzyTests
    {
        [Fact]
        public static void IsMatch_Returns_True_For_Empty_String()
        {
            var include = Fuzzy.Include(new[] { "" });
            var exclude = Fuzzy.Exclude(new[] { "" });

            Assert.True(include.IsMatch("some text!"));
            Assert.True(exclude.IsMatch("some text!"));
        }

        [Fact]
        public static void IsMatch_Matches_Empty_Collection_Based_On_Type()
        {
            var includeAll = Fuzzy.IncludeAll;
            var include = Fuzzy.Include(Array.Empty<string>());

            var excludeNothing = Fuzzy.ExcludeNothing;
            var exclude = Fuzzy.Exclude(Array.Empty<string>());

            Assert.True(includeAll.IsMatch("some text!"));
            Assert.True(include.IsMatch("some text!"));

            Assert.False(excludeNothing.IsMatch("some text!"));
            Assert.False(exclude.IsMatch("some text!"));
        }

        [Fact]
        public static void IsMatch_Treats_Spaces_As_Wildcards()
        {
            var fuzzy = Fuzzy.Include(
                new[] { "He ld", "HE LD" });

            Assert.True(fuzzy.IsMatch("Hello World!"));
            Assert.True(fuzzy.IsMatch("Held"));
            Assert.True(fuzzy.IsMatch("HELLO WORLD!"));

            Assert.False(fuzzy.IsMatch("hello world!"));
            Assert.False(fuzzy.IsMatch("Goodbye World!"));
        }
    }
}
