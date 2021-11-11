namespace Chunkyard.Tests.Core;

public static class FuzzyTests
{
    [Fact]
    public static void IsMatch_Returns_True_For_Empty_String()
    {
        var fuzzy = new Fuzzy(new[] { "" });

        Assert.True(fuzzy.IsIncludingMatch("some text!"));
        Assert.True(fuzzy.IsExcludingMatch("some text!"));
    }

    [Fact]
    public static void IsMatch_Matches_Empty_Collection_Based_On_Type()
    {
        var fuzzy = Fuzzy.Default;

        Assert.True(fuzzy.IsIncludingMatch("some text!"));
        Assert.True(fuzzy.IsIncludingMatch("some text!"));

        Assert.False(fuzzy.IsExcludingMatch("some text!"));
        Assert.False(fuzzy.IsExcludingMatch("some text!"));
    }

    [Theory]
    [InlineData("Hello World!", true)]
    [InlineData("Held", true)]
    [InlineData("HELLO WORLD!", true)]
    [InlineData("hello world!", false)]
    [InlineData("Goodbye World!", false)]
    public static void IsMatch_Treats_Spaces_As_Wildcards(
        string input,
        bool expected)
    {
        var fuzzy = new Fuzzy(
            new[] { "He ld", "HE LD" });

        Assert.Equal(expected, fuzzy.IsIncludingMatch(input));
        Assert.Equal(expected, fuzzy.IsExcludingMatch(input));
    }
}
