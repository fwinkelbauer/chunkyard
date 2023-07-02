namespace Chunkyard.Tests.Cli;

public static class CommandParserTests
{
    [Fact]
    public static void Parse_Returns_Parsed_Command()
    {
        var parser = new CommandParser(
            new SomeCommandParser());

        Assert.IsType<SomeCommand>(
            parser.Parse("cmd"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("help")]
    [InlineData("cmd --help")]
    public static void Parse_Returns_Help_For_Unknown_Or_Help_Command(
        string args)
    {
        var parser = new CommandParser(
            new SomeCommandParser());

        Assert.IsType<HelpCommand>(
            parser.Parse(args.Split(' ')));
    }

    internal sealed class SomeCommand
    {
    }

    internal sealed class SomeCommandParser : ICommandParser
    {
        public string Command => "cmd";

        public string Info => "some info";

        public object Parse(FlagConsumer consumer)
        {
            return new SomeCommand();
        }
    }
}
