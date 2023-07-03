namespace Chunkyard.Tests.Cli;

public static class CommandParserTests
{
    [Theory]
    [InlineData("cmd")]
    [InlineData("cmd --help false")]
    public static void Parse_Returns_Parsed_Command(string args)
    {
        var parser = new CommandParser(
            new SomeCommandParser());

        Assert.IsType<SomeCommand>(
            parser.Parse(args.Split(' ')));
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
