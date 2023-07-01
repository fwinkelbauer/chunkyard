namespace Chunkyard;

public abstract class Command
{
    public const Prompt DefaultPrompt = Prompt.Console;
    public const int DefaultParallel = 1;

    protected Command(
        string repository,
        Prompt? prompt = null,
        int? parallel = null)
    {
        Repository = repository;
        Prompt = prompt ?? DefaultPrompt;
        Parallel = parallel ?? DefaultParallel;
    }

    public string Repository { get; }

    public Prompt Prompt { get; }

    public int Parallel { get; }

    public abstract void Handle(ICommandHandler handler);
}

public sealed class CatCommand : Command
{
    public CatCommand(
        string repository,
        Prompt prompt,
        int snapshotId,
        IEnumerable<string> chunkIds,
        string? export)
        : base(repository, prompt)
    {
        SnapshotId = snapshotId;
        ChunkIds = chunkIds;
        Export = export;
    }

    public int SnapshotId { get; }

    public IEnumerable<string> ChunkIds { get; }

    public string? Export { get; }

    public override void Handle(ICommandHandler handler)
        => handler.Handle(this);
}

public sealed class CheckCommand : Command
{
    public CheckCommand(
        string repository,
        Prompt prompt,
        int parallel,
        int snapshotId,
        IEnumerable<string> includePatterns,
        bool shallow)
        : base(repository, prompt, parallel)
    {
        SnapshotId = snapshotId;
        IncludePatterns = includePatterns;
        Shallow = shallow;
    }

    public int SnapshotId { get; }

    public IEnumerable<string> IncludePatterns { get; }

    public bool Shallow { get; }

    public override void Handle(ICommandHandler handler)
        => handler.Handle(this);
}

public sealed class CopyCommand : Command
{
    public CopyCommand(
        string repository,
        Prompt prompt,
        int parallel,
        string destinationRepository)
        : base(repository, prompt, parallel)
    {
        DestinationRepository = destinationRepository;
    }

    public string DestinationRepository { get; }

    public override void Handle(ICommandHandler handler)
        => handler.Handle(this);
}

public sealed class DiffCommand : Command
{
    public DiffCommand(
        string repository,
        Prompt prompt,
        int firstSnapshotId,
        int secondSnapshotId,
        IEnumerable<string> includePatterns,
        bool chunksOnly)
        : base(repository, prompt)
    {
        FirstSnapshotId = firstSnapshotId;
        SecondSnapshotId = secondSnapshotId;
        IncludePatterns = includePatterns;
        ChunksOnly = chunksOnly;
    }

    public int FirstSnapshotId { get; }

    public int SecondSnapshotId { get; }

    public IEnumerable<string> IncludePatterns { get; }

    public bool ChunksOnly { get; }

    public override void Handle(ICommandHandler handler)
        => handler.Handle(this);
}

public sealed class GarbageCollectCommand : Command
{
    public GarbageCollectCommand(
        string repository,
        Prompt prompt)
        : base(repository, prompt)
    {
    }

    public override void Handle(ICommandHandler handler)
        => handler.Handle(this);
}

public sealed class HelpCommand : Command
{
    public HelpCommand(
        IReadOnlyCollection<Usage> usages,
        IReadOnlyCollection<string> errors)
        : base("")
    {
        Usages = usages;
        Errors = errors;
    }

    public IReadOnlyCollection<Usage> Usages { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public override void Handle(ICommandHandler handler)
        => handler.Handle(this);
}

public sealed class KeepCommand : Command
{
    public KeepCommand(
        string repository,
        Prompt prompt,
        int latestCount)
        : base(repository, prompt)
    {
        LatestCount = latestCount;
    }

    public int LatestCount { get; }

    public override void Handle(ICommandHandler handler)
        => handler.Handle(this);
}

public sealed class ListCommand : Command
{
    public ListCommand(
        string repository,
        Prompt prompt)
        : base(repository, prompt)
    {
    }

    public override void Handle(ICommandHandler handler)
        => handler.Handle(this);
}

public sealed class RemoveCommand : Command
{
    public RemoveCommand(
        string repository,
        Prompt prompt,
        int snapshotId)
        : base(repository, prompt)
    {
        SnapshotId = snapshotId;
    }

    public int SnapshotId { get; }

    public override void Handle(ICommandHandler handler)
        => handler.Handle(this);
}

public sealed class RestoreCommand : Command
{
    public RestoreCommand(
        string repository,
        Prompt prompt,
        int parallel,
        string directory,
        int snapshotId,
        IEnumerable<string> includePatterns,
        bool preview)
        : base(repository, prompt, parallel)
    {
        Directory = directory;
        SnapshotId = snapshotId;
        IncludePatterns = includePatterns;
        Preview = preview;
    }

    public string Directory { get; }

    public int SnapshotId { get; }

    public IEnumerable<string> IncludePatterns { get; }

    public bool Preview { get; }

    public override void Handle(ICommandHandler handler)
        => handler.Handle(this);
}

public sealed class ShowCommand : Command
{
    public ShowCommand(
        string repository,
        Prompt prompt,
        int snapshotId,
        IEnumerable<string> includePatterns,
        bool chunksOnly)
        : base(repository, prompt)
    {
        SnapshotId = snapshotId;
        IncludePatterns = includePatterns;
        ChunksOnly = chunksOnly;
    }

    public int SnapshotId { get; }

    public IEnumerable<string> IncludePatterns { get; }

    public bool ChunksOnly { get; }

    public override void Handle(ICommandHandler handler)
        => handler.Handle(this);
}

public sealed class StoreCommand : Command
{
    public StoreCommand(
        string repository,
        Prompt prompt,
        int parallel,
        IReadOnlyCollection<string> paths,
        IReadOnlyCollection<string> includePatterns,
        bool preview)
        : base(repository, prompt, parallel)
    {
        Paths = paths;
        IncludePatterns = includePatterns;
        Preview = preview;
    }

    public IEnumerable<string> Paths { get; }

    public IEnumerable<string> IncludePatterns { get; }

    public bool Preview { get; }

    public override void Handle(ICommandHandler handler)
        => handler.Handle(this);
}
