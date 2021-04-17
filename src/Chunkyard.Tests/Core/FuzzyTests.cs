using System;
using Chunkyard.Core;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class FuzzyTests
    {
        [Fact]
        public static void Empty_Pattern_Matches_Everything()
        {
            var fuzzy1 = Fuzzy.MatchAll;
            var fuzzy2 = new Fuzzy(new[] { "" }, emptyMatches: true);
            var fuzzy3 = new Fuzzy(new[] { "" }, emptyMatches: false);

            Assert.True(fuzzy1.IsMatch("some text!"));
            Assert.True(fuzzy2.IsMatch("some text!"));
            Assert.True(fuzzy3.IsMatch("some text!"));
        }

        [Fact]
        public static void No_Parameter_Matches_Maybe()
        {
            var fuzzy1 = new Fuzzy(Array.Empty<string>(), emptyMatches: true);
            var fuzzy2 = new Fuzzy(Array.Empty<string>(), emptyMatches: false);
            var fuzzy3 = Fuzzy.MatchNothing;

            Assert.True(fuzzy1.IsMatch("some text!"));
            Assert.False(fuzzy2.IsMatch("some text!"));
            Assert.False(fuzzy3.IsMatch("some text!"));
        }

        [Fact]
        public static void Spaces_Are_Treated_As_Wildcards()
        {
            var fuzzy = new Fuzzy(
                new[] { "He ld", "HE LD" },
                emptyMatches: true);

            Assert.True(fuzzy.IsMatch("Hello World!"));
            Assert.True(fuzzy.IsMatch("Held"));
            Assert.True(fuzzy.IsMatch("HELLO WORLD!"));
            Assert.False(fuzzy.IsMatch("hello world!"));
            Assert.False(fuzzy.IsMatch("Goodbye World!"));
        }
    }
}
