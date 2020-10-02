using Xunit;

namespace Chunkyard.Tests
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
        public static void SpacedPattern_Matches_Everything()
        {
            var fuzzy = new Fuzzy("He ld");

            Assert.True(fuzzy.IsMatch("Hello World!"));
            Assert.False(fuzzy.IsMatch("Goodbye World!"));
        }
    }
}
