namespace Chunkyard.Tests.Core;

public static class FuzzyTests
{
    [Fact]
    public static void IsMatch_Returns_True_For_Empty_Collection_Or_Empty_String()
    {
        var text = "some text!";

        Assert.True(new Fuzzy().IsMatch(text));
        Assert.True(new Fuzzy("").IsMatch(text));
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
        var fuzzy = new Fuzzy("He ld", "HE LD");

        Assert.Equal(expected, fuzzy.IsMatch(input));
    }

    [Fact]
    public static void IsMatch_Treats_Lowercase_As_Ignore_Case()
    {
        var lowerFuzzy = new Fuzzy("hello");
        var upperFuzzy = new Fuzzy("Hello");

        Assert.True(lowerFuzzy.IsMatch("hello"));
        Assert.True(lowerFuzzy.IsMatch("Hello"));

        Assert.False(upperFuzzy.IsMatch("hello"));
        Assert.True(upperFuzzy.IsMatch("Hello"));
    }

    [Fact]
    public static void IsMatch_Excludes_Inverted_Pattern()
    {
        var fuzzy1 = new Fuzzy("hello", "!world");
        var fuzzy2 = new Fuzzy("!world", "!something");
        var fuzzy3 = new Fuzzy(".*", "!world", "!something");

        Assert.True(fuzzy1.IsMatch("Hello planet"));
        Assert.False(fuzzy1.IsMatch("Hello world"));
        Assert.False(fuzzy1.IsMatch("Goodbye"));

        Assert.True(fuzzy2.IsMatch("Hello planet"));
        Assert.False(fuzzy2.IsMatch("Hello world"));
        Assert.True(fuzzy2.IsMatch("Goodbye"));

        Assert.True(fuzzy3.IsMatch("Hello planet"));
        Assert.False(fuzzy3.IsMatch("Hello world"));
        Assert.True(fuzzy3.IsMatch("Goodbye"));
    }

    [Fact]
    public static void IsMatch_Includes_All_When_First_Pattern_Is_Negated()
    {
        var fuzzy = new Fuzzy("!mp3");

        Assert.True(fuzzy.IsMatch("picture.jpg"));
        Assert.False(fuzzy.IsMatch("music.mp3"));
    }

    [Fact]
    public static void IsMatch_Lets_Pattern_Overwrite_Previous_Pattern()
    {
        var fuzzy = new Fuzzy("!mp3", "cool mp3");

        Assert.True(fuzzy.IsMatch("picture.jpg"));
        Assert.False(fuzzy.IsMatch("music.mp3"));
        Assert.True(fuzzy.IsMatch("cool-music.mp3"));
    }
}
