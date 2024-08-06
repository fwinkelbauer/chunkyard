namespace Chunkyard.Tests.Cli;

public sealed class DummyCommandParser : ICommandParser
{
    public DummyCommandParser(string command)
    {
        Command = command;
        Info = command;
    }

    public string Command { get; }

    public string Info { get; }

    public object Parse(FlagConsumer consumer)
    {
        return consumer.NoHelp(out var help)
            ? Command
            : help;
    }
}
