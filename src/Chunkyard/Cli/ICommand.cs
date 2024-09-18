namespace Chunkyard.Cli;

/// <summary>
/// A runnable command which returns an exit code.
/// </summary>
public interface ICommand
{
    int Run();
}
