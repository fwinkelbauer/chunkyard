namespace Chunkyard.Cli;

/// <summary>
/// An interface to describe a specific command line parser.
/// </summary>
public interface ICommandParser
{
    string Command { get; }

    string Info { get; }

    ICommand? Parse(FlagConsumer consumer);
}
