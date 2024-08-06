namespace Chunkyard.Cli;

/// <summary>
/// A basic implementation of <see cref="ICommandParser"/>.
/// </summary>
public sealed class SimpleCommandParser : ICommandParser
{
    private readonly object _result;

    public SimpleCommandParser(
        string command,
        string info,
        object result)
    {
        Command = command;
        Info = info;

        _result = result;
    }

    public string Command { get; }

    public string Info { get; }

    public object Parse(FlagConsumer consumer)
    {
        return consumer.NoHelp(out var help)
            ? _result
            : help;
    }
}
