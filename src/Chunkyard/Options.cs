namespace Chunkyard;

[Verb("cat", HelpText = "Export or print the value of a set of content URIs.")]
public class CatOptions
{
    public CatOptions(
        string repository,
        IEnumerable<Uri> contentUris,
        string? export)
    {
        Repository = repository;
        ContentUris = contentUris;
        Export = export;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path")]
    public string Repository { get; }

    [Option('c', "content", Required = true, HelpText = "The content URIs")]
    public IEnumerable<Uri> ContentUris { get; }

    [Option('e', "export", Required = false, HelpText = "The export path", Default = "")]
    public string? Export { get; }
}

[Verb("check", HelpText = "Check if a snapshot is valid.")]
public class CheckOptions
{
    public CheckOptions(
        string repository,
        int snapshotId,
        IEnumerable<string> includePatterns,
        bool shallow)
    {
        Repository = repository;
        SnapshotId = snapshotId;
        IncludePatterns = includePatterns;
        Shallow = shallow;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path")]
    public string Repository { get; }

    [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID", Default = SnapshotStore.LatestSnapshotId)]
    public int SnapshotId { get; }

    [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for files to include")]
    public IEnumerable<string> IncludePatterns { get; }

    [Option("shallow", Required = false, HelpText = "Check if content exists without further validation", Default = false)]
    public bool Shallow { get; }
}

[Verb("copy", HelpText = "Copy/Mirror snapshots from one repository to another.")]
public class CopyOptions
{
    public CopyOptions(
        string sourceRepository,
        string destinationRepository,
        bool mirror)
    {
        SourceRepository = sourceRepository;
        DestinationRepository = destinationRepository;
        Mirror = mirror;
    }

    [Option('s', "source", Required = true, HelpText = "The source repository path")]
    public string SourceRepository { get; }

    [Option('d', "destination", Required = true, HelpText = "The destination repository path")]
    public string DestinationRepository { get; }

    [Option("mirror", Required = false, HelpText = "Let the destination mirror the source repository", Default = false)]
    public bool Mirror { get; }
}

[Verb("create", HelpText = "Create a new snapshot.")]
public class CreateOptions
{
    public CreateOptions(
        string repository,
        IEnumerable<string> files,
        IEnumerable<string> excludePatterns)
    {
        Repository = repository;
        Files = files;
        ExcludePatterns = excludePatterns;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path")]
    public string Repository { get; }

    [Option('f', "files", Required = true, HelpText = "The files and directories to include")]
    public IEnumerable<string> Files { get; }

    [Option('e', "exclude", Required = false, HelpText = "The fuzzy patterns for files to exclude")]
    public IEnumerable<string> ExcludePatterns { get; }
}

[Verb("diff", HelpText = "Show the difference between two snapshots.")]
public class DiffOptions
{
    public DiffOptions(
        string repository,
        int firstSnapshotId,
        int secondSnapshotId,
        IEnumerable<string> includePatterns,
        bool contentOnly)
    {
        Repository = repository;
        FirstSnapshotId = firstSnapshotId;
        SecondSnapshotId = secondSnapshotId;
        IncludePatterns = includePatterns;
        ContentOnly = contentOnly;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path")]
    public string Repository { get; }

    [Option('f', "first", Required = false, HelpText = "The first snapshot ID", Default = SnapshotStore.SecondLatestSnapshotId)]
    public int FirstSnapshotId { get; }

    [Option('s', "second", Required = false, HelpText = "The second snapshot ID", Default = SnapshotStore.LatestSnapshotId)]
    public int SecondSnapshotId { get; }

    [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for files to include")]
    public IEnumerable<string> IncludePatterns { get; }

    [Option("content-only", Required = false, HelpText = "Show content", Default = false)]
    public bool ContentOnly { get; }
}

[Verb("gc", HelpText = "Remove unreferenced content.")]
public class GarbageCollectOptions
{
    public GarbageCollectOptions(string repository)
    {
        Repository = repository;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path")]
    public string Repository { get; }
}

[Verb("keep", HelpText = "Keep only the given list of snapshots.")]
public class KeepOptions
{
    public KeepOptions(
        string repository,
        int latestCount)
    {
        Repository = repository;
        LatestCount = latestCount;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path")]
    public string Repository { get; }

    [Option("latest", Required = true, HelpText = "The count of the latest snapshots to keep")]
    public int LatestCount { get; }
}

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

[Verb("preview", HelpText = "Preview the output of a create command.")]
public class PreviewOptions
{
    public PreviewOptions(
        string repository,
        IEnumerable<string> files,
        IEnumerable<string> excludePatterns)
    {
        Repository = repository;
        Files = files;
        ExcludePatterns = excludePatterns;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path")]
    public string Repository { get; }

    [Option('f', "files", Required = true, HelpText = "The files and directories to include")]
    public IEnumerable<string> Files { get; }

    [Option('e', "exclude", Required = false, HelpText = "The fuzzy patterns for files to exclude")]
    public IEnumerable<string> ExcludePatterns { get; }
}

[Verb("remove", HelpText = "Remove a snapshot.")]
public class RemoveOptions
{
    public RemoveOptions(
        string repository,
        int snapshotId)
    {
        Repository = repository;
        SnapshotId = snapshotId;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path")]
    public string Repository { get; }

    [Option('s', "snapshot", Required = true, HelpText = "The snapshot ID")]
    public int SnapshotId { get; }
}

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

[Verb("show", HelpText = "Show the content of a snapshot.")]
public class ShowOptions
{
    public ShowOptions(
        string repository,
        int snapshotId,
        IEnumerable<string> includePatterns,
        bool contentOnly)
    {
        Repository = repository;
        SnapshotId = snapshotId;
        IncludePatterns = includePatterns;
        ContentOnly = contentOnly;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path")]
    public string Repository { get; }

    [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID", Default = SnapshotStore.LatestSnapshotId)]
    public int SnapshotId { get; }

    [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for files to include")]
    public IEnumerable<string> IncludePatterns { get; }

    [Option("content-only", Required = false, HelpText = "Show content", Default = false)]
    public bool ContentOnly { get; }
}
