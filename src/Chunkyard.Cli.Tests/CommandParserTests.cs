namespace Chunkyard.Cli.Tests;

public static class CommandParserTests
{
    [Theory]
    [InlineData("cmd")]
    [InlineData("cmd --help false")]
    public static void Parse_Returns_Parsed_Command(string args)
    {
        var expected = "the result";

        var parser = new CommandParser(
            new[]
            {
                new SimpleCommandParser("cmd", "info", expected)
            });

        Assert.Equal(expected, parser.Parse(args.Split(' ')));
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("help")]
    [InlineData("cmd --unknown value")]
    [InlineData("cmd --help")]
    [InlineData("cmd --help true")]
    [InlineData("cmd --help invalid")]
    public static void Parse_Returns_Help_For_Unknown_Invalid_Or_Help_Command(
        string args)
    {
        var parser = new CommandParser(
            new[]
            {
                new SimpleCommandParser("cmd", "info", "the result")
            });

        Assert.IsType<HelpCommand>(
            parser.Parse(args.Split(' ')));
    }

    [Fact]
    public static void Parse_Dispatches_To_Correct_Parser()
    {
        var parser = new CommandParser(
            new[]
            {
                new SimpleCommandParser("one", "info", "result-one"),
                new SimpleCommandParser("two", "info", "result-two"),
                new SimpleCommandParser("three", "info", "result-three")
            });

        Assert.Equal("result-one", parser.Parse("one"));
        Assert.Equal("result-two", parser.Parse("two"));
        Assert.Equal("result-three", parser.Parse("three"));
    }
}
