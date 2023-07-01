namespace Chunkyard.Cli;

public interface ICommandParser
{
    string Command { get; }

    string Info { get; }

    ICommand Parse(ArgConsumer consumer);
}
