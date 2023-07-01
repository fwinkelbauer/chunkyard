namespace Chunkyard.Cli;

public interface ICommandParser
{
    string Command { get; }

    string Info { get; }

    object Parse(ArgConsumer consumer);
}
