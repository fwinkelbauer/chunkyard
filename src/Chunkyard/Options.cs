namespace Chunkyard;

[Verb("cat", HelpText = "Export or print the value of a set of chunk IDs.")]
public class CatOptions
{
    public CatOptions(
        string repository,
        IEnumerable<Uri> chunkIds,
        string? export)
    {
        Repository = repository;
        ChunkIds = chunkIds;
        Export = export;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path.")]
    public string Repository { get; }

    [Option('c', "chunk", Required = true, HelpText = "The chunk IDs.")]
    public IEnumerable<Uri> ChunkIds { get; }

    [Option('e', "export", Required = false, HelpText = "The export path.", Default = "")]
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

    [Option('r', "repository", Required = true, HelpText = "The repository path.")]
    public string Repository { get; }

    [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID.", Default = SnapshotStore.LatestSnapshotId)]
    public int SnapshotId { get; }

    [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for files to include.")]
    public IEnumerable<string> IncludePatterns { get; }

    [Option("shallow", Required = false, HelpText = "Only check if chunks exist.", Default = false)]
    public bool Shallow { get; }
}

[Verb("copy", HelpText = "Copy snapshots from one repository to another.")]
public class CopyOptions
{
    public CopyOptions(
        string sourceRepository,
        string destinationRepository)
    {
        SourceRepository = sourceRepository;
        DestinationRepository = destinationRepository;
    }

    [Option('s', "source", Required = true, HelpText = "The source repository path.")]
    public string SourceRepository { get; }

    [Option('d', "destination", Required = true, HelpText = "The destination repository path.")]
    public string DestinationRepository { get; }
}

[Verb("store", HelpText = "Store a new snapshot.")]
public class StoreOptions
{
    public StoreOptions(
        string repository,
        IEnumerable<string> files,
        IEnumerable<string> excludePatterns,
        bool preview)
    {
        Repository = repository;
        Files = files;
        ExcludePatterns = excludePatterns;
        Preview = preview;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path.")]
    public string Repository { get; }

    [Option('f', "files", Required = true, HelpText = "The files and directories to include.")]
    public IEnumerable<string> Files { get; }

    [Option('e', "exclude", Required = false, HelpText = "The fuzzy patterns for files to exclude.")]
    public IEnumerable<string> ExcludePatterns { get; }

    [Option("preview", Required = false, HelpText = "Show only a preview.")]
    public bool Preview { get; }
}

[Verb("diff", HelpText = "Show the difference between two snapshots.")]
public class DiffOptions
{
    public DiffOptions(
        string repository,
        int firstSnapshotId,
        int secondSnapshotId,
        IEnumerable<string> includePatterns,
        bool chunksOnly)
    {
        Repository = repository;
        FirstSnapshotId = firstSnapshotId;
        SecondSnapshotId = secondSnapshotId;
        IncludePatterns = includePatterns;
        ChunksOnly = chunksOnly;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path.")]
    public string Repository { get; }

    [Option('f', "first", Required = false, HelpText = "The first snapshot ID.", Default = SnapshotStore.SecondLatestSnapshotId)]
    public int FirstSnapshotId { get; }

    [Option('s', "second", Required = false, HelpText = "The second snapshot ID.", Default = SnapshotStore.LatestSnapshotId)]
    public int SecondSnapshotId { get; }

    [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for files to include.")]
    public IEnumerable<string> IncludePatterns { get; }

    [Option("chunks-only", Required = false, HelpText = "Show chunk IDs.", Default = false)]
    public bool ChunksOnly { get; }
}

[Verb("gc", HelpText = "Remove unreferenced chunks.")]
public class GarbageCollectOptions
{
    public GarbageCollectOptions(string repository)
    {
        Repository = repository;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path.")]
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

    [Option('r', "repository", Required = true, HelpText = "The repository path.")]
    public string Repository { get; }

    [Option("latest", Required = true, HelpText = "The count of the latest snapshots to keep.")]
    public int LatestCount { get; }
}

[Verb("list", HelpText = "List all snapshots.")]
public class ListOptions
{
    public ListOptions(string repository)
    {
        Repository = repository;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path.")]
    public string Repository { get; }
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

    [Option('r', "repository", Required = true, HelpText = "The repository path.")]
    public string Repository { get; }

    [Option('s', "snapshot", Required = true, HelpText = "The snapshot ID.")]
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

    [Option('r', "repository", Required = true, HelpText = "The repository path.")]
    public string Repository { get; }

    [Option('d', "directory", Required = false, HelpText = "The directory to restore into.", Default = ".")]
    public string Directory { get; }

    [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID.", Default = SnapshotStore.LatestSnapshotId)]
    public int SnapshotId { get; }

    [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for files to include.")]
    public IEnumerable<string> IncludePatterns { get; }
}

[Verb("mirror", HelpText = "Restore, overwrite and remove files to mirror a snapshot.")]
public class MirrorOptions
{
    public MirrorOptions(
        string repository,
        IEnumerable<string> files,
        IEnumerable<string> excludePatterns,
        int snapshotId,
        bool preview)
    {
        Repository = repository;
        Files = files;
        ExcludePatterns = excludePatterns;
        SnapshotId = snapshotId;
        Preview = preview;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path.")]
    public string Repository { get; }

    [Option('f', "files", Required = true, HelpText = "The files and directories to include.")]
    public IEnumerable<string> Files { get; }

    [Option('e', "exclude", Required = false, HelpText = "The fuzzy patterns for files to exclude.")]
    public IEnumerable<string> ExcludePatterns { get; }

    [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID.", Default = SnapshotStore.LatestSnapshotId)]
    public int SnapshotId { get; }

    [Option("preview", Required = false, HelpText = "Show only a preview.")]
    public bool Preview { get; }
}

[Verb("show", HelpText = "Show the content of a snapshot.")]
public class ShowOptions
{
    public ShowOptions(
        string repository,
        int snapshotId,
        IEnumerable<string> includePatterns,
        bool chunksOnly)
    {
        Repository = repository;
        SnapshotId = snapshotId;
        IncludePatterns = includePatterns;
        ChunksOnly = chunksOnly;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path.")]
    public string Repository { get; }

    [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID.", Default = SnapshotStore.LatestSnapshotId)]
    public int SnapshotId { get; }

    [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for files to include.")]
    public IEnumerable<string> IncludePatterns { get; }

    [Option("chunks-only", Required = false, HelpText = "Show chunk IDs.", Default = false)]
    public bool ChunksOnly { get; }
}
