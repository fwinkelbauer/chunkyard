namespace Chunkyard.Tests.Core;

[TestClass]
public sealed class FuzzyTests
{
    [TestMethod]
    public void IsMatch_Returns_True_For_Empty_Collection_Or_Empty_String()
    {
        var text = "some text!";

        Assert.IsTrue(new Fuzzy().IsMatch(text));
        Assert.IsTrue(new Fuzzy("").IsMatch(text));
    }

    [TestMethod]
    [DataRow("Hello World!", true)]
    [DataRow("Held", true)]
    [DataRow("HELLO WORLD!", true)]
    [DataRow("hello world!", false)]
    [DataRow("Goodbye World!", false)]
    public void IsMatch_Treats_Spaces_As_Wildcards(
        string input,
        bool expected)
    {
        var fuzzy = new Fuzzy("He ld", "HE LD");

        Assert.AreEqual(expected, fuzzy.IsMatch(input));
    }

    [TestMethod]
    public void IsMatch_Treats_Lowercase_As_Ignore_Case()
    {
        var lowerFuzzy = new Fuzzy("hello");
        var upperFuzzy = new Fuzzy("Hello");

        Assert.IsTrue(lowerFuzzy.IsMatch("hello"));
        Assert.IsTrue(lowerFuzzy.IsMatch("Hello"));

        Assert.IsFalse(upperFuzzy.IsMatch("hello"));
        Assert.IsTrue(upperFuzzy.IsMatch("Hello"));
    }

    [TestMethod]
    public void IsMatch_Excludes_Inverted_Pattern()
    {
        var fuzzy1 = new Fuzzy("hello", "!world");
        var fuzzy2 = new Fuzzy("!world", "!something");
        var fuzzy3 = new Fuzzy(".*", "!world", "!something");

        Assert.IsTrue(fuzzy1.IsMatch("Hello planet"));
        Assert.IsFalse(fuzzy1.IsMatch("Hello world"));
        Assert.IsFalse(fuzzy1.IsMatch("Goodbye"));

        Assert.IsTrue(fuzzy2.IsMatch("Hello planet"));
        Assert.IsFalse(fuzzy2.IsMatch("Hello world"));
        Assert.IsTrue(fuzzy2.IsMatch("Goodbye"));

        Assert.IsTrue(fuzzy3.IsMatch("Hello planet"));
        Assert.IsFalse(fuzzy3.IsMatch("Hello world"));
        Assert.IsTrue(fuzzy3.IsMatch("Goodbye"));
    }

    [TestMethod]
    public void IsMatch_Includes_All_When_First_Pattern_Is_Negated()
    {
        var fuzzy = new Fuzzy("!mp3");

        Assert.IsTrue(fuzzy.IsMatch("picture.jpg"));
        Assert.IsFalse(fuzzy.IsMatch("music.mp3"));
    }

    [TestMethod]
    public void IsMatch_Lets_Pattern_Overwrite_Previous_Pattern()
    {
        var fuzzy = new Fuzzy("!mp3", "cool mp3");

        Assert.IsTrue(fuzzy.IsMatch("picture.jpg"));
        Assert.IsFalse(fuzzy.IsMatch("music.mp3"));
        Assert.IsTrue(fuzzy.IsMatch("cool-music.mp3"));
    }
}
