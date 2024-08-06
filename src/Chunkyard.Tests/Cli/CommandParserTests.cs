namespace Chunkyard.Tests.Cli;

public static class CommandParserTests
{
    [Fact]
    public static void Parse_Dispatches_To_Correct_Parser()
    {
        var parser = new CommandParser(
            new[]
            {
                new DummyCommandParser("one"),
                new DummyCommandParser("two"),
                new DummyCommandParser("three")
            });

        Assert.Equal("one", parser.Parse("one"));
        Assert.Equal("one", parser.Parse("one", "--help", "false"));
        Assert.Equal("two", parser.Parse("two"));
        Assert.Equal("three", parser.Parse("three"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown")]
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
                new DummyCommandParser("cmd")
            });

        Assert.IsType<HelpCommand>(
            parser.Parse(args.Split(' ')));
    }
}
