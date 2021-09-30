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
            var fuzzy1 = new Fuzzy(
                new[] { "" },
                FuzzyOption.EmptyMatchesAll);

            var fuzzy2 = new Fuzzy(
                new[] { "" },
                FuzzyOption.EmptyMatchesNothing);

            Assert.True(fuzzy1.IsMatch("some text!"));
            Assert.True(fuzzy2.IsMatch("some text!"));
        }

        [Fact]
        public static void IsMatch_Matches_Empty_Collection_Based_On_FuzzyOption()
        {
            var fuzzy1 = Fuzzy.MatchAll;
            var fuzzy2 = new Fuzzy(
                Array.Empty<string>(),
                FuzzyOption.EmptyMatchesAll);

            var fuzzy3 = Fuzzy.MatchNothing;
            var fuzzy4 = new Fuzzy(
                Array.Empty<string>(),
                FuzzyOption.EmptyMatchesNothing);

            Assert.True(fuzzy1.IsMatch("some text!"));
            Assert.True(fuzzy2.IsMatch("some text!"));

            Assert.False(fuzzy3.IsMatch("some text!"));
            Assert.False(fuzzy4.IsMatch("some text!"));
        }

        [Fact]
        public static void IsMatch_Treats_Spaces_As_Wildcards()
        {
            var fuzzy = new Fuzzy(
                new[] { "He ld", "HE LD" },
                FuzzyOption.EmptyMatchesAll);

            Assert.True(fuzzy.IsMatch("Hello World!"));
            Assert.True(fuzzy.IsMatch("Held"));
            Assert.True(fuzzy.IsMatch("HELLO WORLD!"));

            Assert.False(fuzzy.IsMatch("hello world!"));
            Assert.False(fuzzy.IsMatch("Goodbye World!"));
        }
    }
}
