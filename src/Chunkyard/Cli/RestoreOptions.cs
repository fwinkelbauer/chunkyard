namespace Chunkyard.Cli;

[Verb("restore", HelpText = "Restore a snapshot.")]
public class RestoreOptions
{
    public RestoreOptions(
        string repository,
        string directory,
        int snapshotId,
        IEnumerable<string> includePatterns)
    {
        Repository = repository;
        Directory = directory;
        SnapshotId = snapshotId;
        IncludePatterns = includePatterns;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path")]
    public string Repository { get; }

    [Option('d', "directory", Required = false, HelpText = "The directory to restore into", Default = ".")]
    public string Directory { get; }

    [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID", Default = SnapshotStore.LatestSnapshotId)]
    public int SnapshotId { get; }

    [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for files to include")]
    public IEnumerable<string> IncludePatterns { get; }
}
