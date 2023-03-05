namespace Chunkyard;

public abstract class Options
{
    protected Options(
        string repository,
        int parallel,
        Prompt prompt)
    {
        Repository = repository;
        Parallel = parallel;
        Prompt = prompt;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path.")]
    public string Repository { get; }

    [Option("parallel", Required = false, HelpText = "The degree of parallelism.", Default = 1)]
    public int Parallel { get; }

    [Option("prompt", Required = false, HelpText = "The password prompt method.", Default = Prompt.Console)]
    public Prompt Prompt { get; }
}

[Verb("cat", HelpText = "Export or print the value of a snapshot or a set of chunk IDs.")]
public sealed class CatOptions : Options
{
    public CatOptions(
        IEnumerable<string> chunkIds,
        int snapshotId,
        string? export,
        string repository,
        int parallel,
        Prompt prompt)
        : base(repository, parallel, prompt)
    {
        ChunkIds = chunkIds;
        SnapshotId = snapshotId;
        Export = export;
    }

    [Option('c', "chunk", Required = false, HelpText = "The chunk IDs.", SetName = "chunks")]
    public IEnumerable<string> ChunkIds { get; }

    [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID.", Default = SnapshotStore.LatestSnapshotId, SetName = "snapshots")]
    public int SnapshotId { get; }

    [Option('e', "export", Required = false, HelpText = "The export path.", Default = "")]
    public string? Export { get; }
}

[Verb("check", HelpText = "Check if a snapshot is valid.")]
public sealed class CheckOptions : Options
{
    public CheckOptions(
        int snapshotId,
        IEnumerable<string> includePatterns,
        bool shallow,
        string repository,
        int parallel,
        Prompt prompt)
        : base(repository, parallel, prompt)
    {
        SnapshotId = snapshotId;
        IncludePatterns = includePatterns;
        Shallow = shallow;
    }

    [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID.", Default = SnapshotStore.LatestSnapshotId)]
    public int SnapshotId { get; }

    [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for blobs to include.")]
    public IEnumerable<string> IncludePatterns { get; }

    [Option("shallow", Required = false, HelpText = "Only check if chunks exist.", Default = false)]
    public bool Shallow { get; }
}

[Verb("copy", HelpText = "Copy snapshots from one repository to another.")]
public sealed class CopyOptions : Options
{
    public CopyOptions(
        string destinationRepository,
        string repository,
        int parallel,
        Prompt prompt)
        : base(repository, parallel, prompt)
    {
        DestinationRepository = destinationRepository;
    }

    [Option('d', "destination", Required = true, HelpText = "The destination repository path.")]
    public string DestinationRepository { get; }
}

[Verb("store", HelpText = "Store a new snapshot.")]
public sealed class StoreOptions : Options
{
    public StoreOptions(
        IEnumerable<string> paths,
        IEnumerable<string> includePatterns,
        bool preview,
        string repository,
        int parallel,
        Prompt prompt)
        : base(repository, parallel, prompt)
    {
        Paths = paths;
        IncludePatterns = includePatterns;
        Preview = preview;
    }

    [Option('p', "paths", Required = true, HelpText = "The files and directories (blobs) to store.")]
    public IEnumerable<string> Paths { get; }

    [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for blobs to include.")]
    public IEnumerable<string> IncludePatterns { get; }

    [Option("preview", Required = false, HelpText = "Show only a preview.")]
    public bool Preview { get; }
}

[Verb("diff", HelpText = "Show the difference between two snapshots.")]
public sealed class DiffOptions : Options
{
    public DiffOptions(
        int firstSnapshotId,
        int secondSnapshotId,
        IEnumerable<string> includePatterns,
        bool chunksOnly,
        string repository,
        int parallel,
        Prompt prompt)
        : base(repository, parallel, prompt)
    {
        FirstSnapshotId = firstSnapshotId;
        SecondSnapshotId = secondSnapshotId;
        IncludePatterns = includePatterns;
        ChunksOnly = chunksOnly;
    }

    [Option('f', "first", Required = false, HelpText = "The first snapshot ID.", Default = SnapshotStore.SecondLatestSnapshotId)]
    public int FirstSnapshotId { get; }

    [Option('s', "second", Required = false, HelpText = "The second snapshot ID.", Default = SnapshotStore.LatestSnapshotId)]
    public int SecondSnapshotId { get; }

    [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for blobs to include.")]
    public IEnumerable<string> IncludePatterns { get; }

    [Option("chunks-only", Required = false, HelpText = "Show chunk IDs.", Default = false)]
    public bool ChunksOnly { get; }
}

[Verb("gc", HelpText = "Remove unreferenced chunks.")]
public sealed class GarbageCollectOptions : Options
{
    public GarbageCollectOptions(
        string repository,
        int parallel,
        Prompt prompt)
        : base(repository, parallel, prompt)
    {
    }
}

[Verb("keep", HelpText = "Keep only the given list of snapshots.")]
public sealed class KeepOptions : Options
{
    public KeepOptions(
        int latestCount,
        string repository,
        int parallel,
        Prompt prompt)
        : base(repository, parallel, prompt)
    {
        LatestCount = latestCount;
    }

    [Option("latest", Required = true, HelpText = "The count of the latest snapshots to keep.")]
    public int LatestCount { get; }
}

[Verb("list", HelpText = "List all snapshots.")]
public sealed class ListOptions : Options
{
    public ListOptions(
        string repository,
        int parallel,
        Prompt prompt)
        : base(repository, parallel, prompt)
    {
    }
}

[Verb("remove", HelpText = "Remove a snapshot.")]
public sealed class RemoveOptions : Options
{
    public RemoveOptions(
        int snapshotId,
        string repository,
        int parallel,
        Prompt prompt)
        : base(repository, parallel, prompt)
    {
        SnapshotId = snapshotId;
    }

    [Option('s', "snapshot", Required = true, HelpText = "The snapshot ID.")]
    public int SnapshotId { get; }
}

[Verb("restore", HelpText = "Restore a snapshot.")]
public sealed class RestoreOptions : Options
{
    public RestoreOptions(
        string directory,
        int snapshotId,
        IEnumerable<string> includePatterns,
        bool preview,
        string repository,
        int parallel,
        Prompt prompt)
        : base(repository, parallel, prompt)
    {
        Directory = directory;
        SnapshotId = snapshotId;
        IncludePatterns = includePatterns;
        Preview = preview;
    }

    [Option('d', "directory", Required = false, HelpText = "The directory to restore into.", Default = ".")]
    public string Directory { get; }

    [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID.", Default = SnapshotStore.LatestSnapshotId)]
    public int SnapshotId { get; }

    [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for blobs to include.")]
    public IEnumerable<string> IncludePatterns { get; }

    [Option("preview", Required = false, HelpText = "Show only a preview.")]
    public bool Preview { get; }
}

[Verb("show", HelpText = "Show the content of a snapshot.")]
public sealed class ShowOptions : Options
{
    public ShowOptions(
        int snapshotId,
        IEnumerable<string> includePatterns,
        bool chunksOnly,
        string repository,
        int parallel,
        Prompt prompt)
        : base(repository, parallel, prompt)
    {
        SnapshotId = snapshotId;
        IncludePatterns = includePatterns;
        ChunksOnly = chunksOnly;
    }

    [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID.", Default = SnapshotStore.LatestSnapshotId)]
    public int SnapshotId { get; }

    [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for blobs to include.")]
    public IEnumerable<string> IncludePatterns { get; }

    [Option("chunks-only", Required = false, HelpText = "Show chunk IDs.", Default = false)]
    public bool ChunksOnly { get; }
}
