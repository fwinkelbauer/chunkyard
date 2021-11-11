namespace Chunkyard.Cli;

[Verb("list", HelpText = "List all snapshots.")]
public class ListOptions
{
    public ListOptions(string repository)
    {
        Repository = repository;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path")]
    public string Repository { get; }
}
