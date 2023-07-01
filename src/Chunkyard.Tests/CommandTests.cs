namespace Chunkyard.Tests;

public static class CommandTests
{
    [Fact]
    public static void CommandParser_Contains_All_Parsers()
    {
        var allParsers = typeof(CommandParser).Assembly.GetTypes()
            .Where(t => t.GetInterface(nameof(ICommandParser)) != null)
            .Select(t => t.Name)
            .ToArray();

        var allCommands = typeof(CommandParser).Assembly.GetTypes()
            .Where(t => t.BaseType != null && t.BaseType == typeof(Command))
            .Select(t => t.Name)
            .ToArray();

        Assert.Equal(
            allParsers,
            CommandParser.Parsers.Select(p => p.GetType().Name));

        Assert.Equal(
            allCommands,
            allParsers.Select(p => p.Replace("Parser", "")));
    }

    [Fact]
    public static void CommandParser_Contains_Distinct_Information()
    {
        var commands = CommandParser.Parsers.Select(p => p.Command).ToArray();
        var infos = CommandParser.Parsers.Select(p => p.Info).ToArray();

        Assert.Equal(commands, commands.Distinct());
        Assert.Equal(infos, infos.Distinct());
    }
}
