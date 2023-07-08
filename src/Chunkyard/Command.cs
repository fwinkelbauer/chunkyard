namespace Chunkyard;

public sealed class CatCommandParser : ICommandParser
{
    public string Command => "cat";

    public string Info => "Export or print the value of a snapshot or a set of chunk IDs";

    public object Parse(FlagConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & (consumer.TrySnapshot(out var snapshotId)
                | consumer.TryStrings("--chunks", "The chunk IDs", out var chunkIds))
            & consumer.TryString("--export", "The export path", out var export, ""))
        {
            return new CatCommand(
                repository,
                prompt,
                parallel,
                snapshotId,
                chunkIds,
                export);
        }
        else
        {
            return consumer.Help;
        }
    }
}

public sealed class CheckCommandParser : ICommandParser
{
    public string Command => "check";

    public string Info => "Check if a snapshot is valid";

    public object Parse(FlagConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TrySnapshot(out var snapshotId)
            & consumer.TryIncludePatterns(out var includePatterns)
            & consumer.TryBool("--shallow", "Only check if chunks exist", out var shallow))
        {
            return new CheckCommand(
                repository,
                prompt,
                parallel,
                snapshotId,
                includePatterns,
                shallow);
        }
        else
        {
            return consumer.Help;
        }
    }
}

public sealed class CopyCommandParser : ICommandParser
{
    public string Command => "copy";

    public string Info => "Copy snapshots from one repository to another";

    public object Parse(FlagConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TryString("--destination", "The destination repository path", out var destinationRepository))
        {
            return new CopyCommand(
                repository,
                prompt,
                parallel,
                destinationRepository);
        }
        else
        {
            return consumer.Help;
        }
    }
}

public sealed class DiffCommandParser : ICommandParser
{
    public string Command => "diff";

    public string Info => "Show the difference between two snapshots";

    public object Parse(FlagConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TryInt("--first", "The first snapshot ID", out var firstSnapshotId, SnapshotStore.SecondLatestSnapshotId)
            & consumer.TryInt("--second", "The second snapshot ID", out var secondSnapshotId, SnapshotStore.LatestSnapshotId)
            & consumer.TryIncludePatterns(out var includePatterns)
            & consumer.TryChunksOnly(out var chunksOnly))
        {
            return new DiffCommand(
                repository,
                prompt,
                parallel,
                firstSnapshotId,
                secondSnapshotId,
                includePatterns,
                chunksOnly);
        }
        else
        {
            return consumer.Help;
        }
    }
}

public sealed class GarbageCollectCommandParser : ICommandParser
{
    public string Command => "gc";

    public string Info => "Remove unreferenced chunks";

    public object Parse(FlagConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel))
        {
            return new GarbageCollectCommand(
                repository,
                prompt,
                parallel);
        }
        else
        {
            return consumer.Help;
        }
    }
}

public sealed class KeepCommandParser : ICommandParser
{
    public string Command => "keep";

    public string Info => "Keep only the given list of snapshots";

    public object Parse(FlagConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TryInt("--latest", "The count of the latest snapshots to keep", out var latestCount))
        {
            return new KeepCommand(
                repository,
                prompt,
                parallel,
                latestCount);
        }
        else
        {
            return consumer.Help;
        }
    }
}

public sealed class ListCommandParser : ICommandParser
{
    public string Command => "list";

    public string Info => "List all snapshots";

    public object Parse(FlagConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel))
        {
            return new ListCommand(
                repository,
                prompt,
                parallel);
        }
        else
        {
            return consumer.Help;
        }
    }
}

public sealed class RemoveCommandParser : ICommandParser
{
    public string Command => "remove";

    public string Info => "Remove a snapshot or a set of chunk IDs";

    public object Parse(FlagConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TrySnapshot(out var snapshot))
        {
            return new RemoveCommand(
                repository,
                prompt,
                parallel,
                snapshot);
        }
        else
        {
            return consumer.Help;
        }
    }
}

public sealed class RestoreCommandParser : ICommandParser
{
    public string Command => "restore";

    public string Info => "Restore a snapshot";

    public object Parse(FlagConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TryString("--directory", "The directory to restore into", out var directory)
            & consumer.TrySnapshot(out var snapshot)
            & consumer.TryIncludePatterns(out var includePatterns)
            & consumer.TryPreview(out var preview))
        {
            return new RestoreCommand(
                repository,
                prompt,
                parallel,
                directory,
                snapshot,
                includePatterns,
                preview);
        }
        else
        {
            return consumer.Help;
        }
    }
}

public sealed class ShowCommandParser : ICommandParser
{
    public string Command => "show";

    public string Info => "Show the content of a snapshot";

    public object Parse(FlagConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TrySnapshot(out var snapshot)
            & consumer.TryIncludePatterns(out var includePatterns)
            & consumer.TryChunksOnly(out var chunksOnly))
        {
            return new ShowCommand(
                repository,
                prompt,
                parallel,
                snapshot,
                includePatterns,
                chunksOnly);
        }
        else
        {
            return consumer.Help;
        }
    }
}

public sealed class StoreCommandParser : ICommandParser
{
    public string Command => "store";

    public string Info => "Store a new snapshot";

    public object Parse(FlagConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TryStrings("--paths", "The files and directories (blobs) to store", out var paths)
            & consumer.TryIncludePatterns(out var includePatterns)
            & consumer.TryPreview(out var preview))
        {
            return new StoreCommand(
                repository,
                prompt,
                parallel,
                paths,
                includePatterns,
                preview);
        }
        else
        {
            return consumer.Help;
        }
    }
}

public interface IChunkyardCommand
{
    string Repository { get; }

    Prompt Prompt { get; }

    int Parallel { get; }
}

public sealed class CatCommand : IChunkyardCommand
{
    public CatCommand(
        string repository,
        Prompt prompt,
        int parallel,
        int snapshotId,
        IEnumerable<string> chunkIds,
        string? export)
    {
        Repository = repository;
        Prompt = prompt;
        Parallel = parallel;
        SnapshotId = snapshotId;
        ChunkIds = chunkIds;
        Export = export;
    }

    public string Repository { get; }

    public Prompt Prompt { get; }

    public int Parallel { get; }

    public int SnapshotId { get; }

    public IEnumerable<string> ChunkIds { get; }

    public string? Export { get; }
}

public sealed class CheckCommand : IChunkyardCommand
{
    public CheckCommand(
        string repository,
        Prompt prompt,
        int parallel,
        int snapshotId,
        IEnumerable<string> includePatterns,
        bool shallow)
    {
        Repository = repository;
        Prompt = prompt;
        Parallel = parallel;
        SnapshotId = snapshotId;
        IncludePatterns = includePatterns;
        Shallow = shallow;
    }

    public string Repository { get; }

    public Prompt Prompt { get; }

    public int Parallel { get; }

    public int SnapshotId { get; }

    public IEnumerable<string> IncludePatterns { get; }

    public bool Shallow { get; }
}

public sealed class CopyCommand : IChunkyardCommand
{
    public CopyCommand(
        string repository,
        Prompt prompt,
        int parallel,
        string destinationRepository)
    {
        Repository = repository;
        Prompt = prompt;
        Parallel = parallel;
        DestinationRepository = destinationRepository;
    }

    public string Repository { get; }

    public Prompt Prompt { get; }

    public int Parallel { get; }

    public string DestinationRepository { get; }
}

public sealed class DiffCommand : IChunkyardCommand
{
    public DiffCommand(
        string repository,
        Prompt prompt,
        int parallel,
        int firstSnapshotId,
        int secondSnapshotId,
        IEnumerable<string> includePatterns,
        bool chunksOnly)
    {
        Repository = repository;
        Prompt = prompt;
        Parallel = parallel;
        FirstSnapshotId = firstSnapshotId;
        SecondSnapshotId = secondSnapshotId;
        IncludePatterns = includePatterns;
        ChunksOnly = chunksOnly;
    }

    public string Repository { get; }

    public Prompt Prompt { get; }

    public int Parallel { get; }

    public int FirstSnapshotId { get; }

    public int SecondSnapshotId { get; }

    public IEnumerable<string> IncludePatterns { get; }

    public bool ChunksOnly { get; }
}

public sealed class GarbageCollectCommand : IChunkyardCommand
{
    public GarbageCollectCommand(
        string repository,
        Prompt prompt,
        int parallel)
    {
        Repository = repository;
        Prompt = prompt;
        Parallel = parallel;
    }

    public string Repository { get; }

    public Prompt Prompt { get; }

    public int Parallel { get; }
}

public sealed class KeepCommand : IChunkyardCommand
{
    public KeepCommand(
        string repository,
        Prompt prompt,
        int parallel,
        int latestCount)
    {
        Repository = repository;
        Prompt = prompt;
        Parallel = parallel;
        LatestCount = latestCount;
    }

    public string Repository { get; }

    public Prompt Prompt { get; }

    public int Parallel { get; }

    public int LatestCount { get; }
}

public sealed class ListCommand : IChunkyardCommand
{
    public ListCommand(
        string repository,
        Prompt prompt,
        int parallel)
    {
        Repository = repository;
        Prompt = prompt;
        Parallel = parallel;
    }

    public string Repository { get; }

    public Prompt Prompt { get; }

    public int Parallel { get; }
}

public sealed class RemoveCommand : IChunkyardCommand
{
    public RemoveCommand(
        string repository,
        Prompt prompt,
        int parallel,
        int snapshotId)
    {
        Repository = repository;
        Prompt = prompt;
        Parallel = parallel;
        SnapshotId = snapshotId;
    }

    public string Repository { get; }

    public Prompt Prompt { get; }

    public int Parallel { get; }

    public int SnapshotId { get; }
}

public sealed class RestoreCommand : IChunkyardCommand
{
    public RestoreCommand(
        string repository,
        Prompt prompt,
        int parallel,
        string directory,
        int snapshotId,
        IEnumerable<string> includePatterns,
        bool preview)
    {
        Repository = repository;
        Prompt = prompt;
        Parallel = parallel;
        Directory = directory;
        SnapshotId = snapshotId;
        IncludePatterns = includePatterns;
        Preview = preview;
    }

    public string Repository { get; }

    public Prompt Prompt { get; }

    public int Parallel { get; }

    public string Directory { get; }

    public int SnapshotId { get; }

    public IEnumerable<string> IncludePatterns { get; }

    public bool Preview { get; }
}

public sealed class ShowCommand : IChunkyardCommand
{
    public ShowCommand(
        string repository,
        Prompt prompt,
        int parallel,
        int snapshotId,
        IEnumerable<string> includePatterns,
        bool chunksOnly)
    {
        Repository = repository;
        Prompt = prompt;
        Parallel = parallel;
        SnapshotId = snapshotId;
        IncludePatterns = includePatterns;
        ChunksOnly = chunksOnly;
    }

    public string Repository { get; }

    public Prompt Prompt { get; }

    public int Parallel { get; }

    public int SnapshotId { get; }

    public IEnumerable<string> IncludePatterns { get; }

    public bool ChunksOnly { get; }
}

public sealed class StoreCommand : IChunkyardCommand
{
    public StoreCommand(
        string repository,
        Prompt prompt,
        int parallel,
        IReadOnlyCollection<string> paths,
        IReadOnlyCollection<string> includePatterns,
        bool preview)
    {
        Repository = repository;
        Prompt = prompt;
        Parallel = parallel;
        Paths = paths;
        IncludePatterns = includePatterns;
        Preview = preview;
    }

    public string Repository { get; }

    public Prompt Prompt { get; }

    public int Parallel { get; }

    public IEnumerable<string> Paths { get; }

    public IEnumerable<string> IncludePatterns { get; }

    public bool Preview { get; }
}


public static class ArgConsumerExtensions
{
    public static bool TryCommon(
        this FlagConsumer consumer,
        out string repository,
        out Prompt prompt,
        out int parallel)
    {
        var prompts = string.Join(", ", Enum.GetNames<Prompt>());

        return consumer.TryString("--repository", "The repository path", out repository)
            & consumer.TryEnum("--prompt", $"The password prompt method: {prompts}", out prompt, Prompt.Console)
            & consumer.TryInt("--parallel", "The degree of parallelism", out parallel, 1);
    }

    public static bool TrySnapshot(
        this FlagConsumer consumer,
        out int snapshot)
    {
        return consumer.TryInt(
            "--snapshot",
            "The snapshot ID",
            out snapshot,
            SnapshotStore.LatestSnapshotId);
    }

    public static bool TryIncludePatterns(
        this FlagConsumer consumer,
        out IReadOnlyCollection<string> includePatterns)
    {
        return consumer.TryStrings(
            "--include",
            "The fuzzy patterns for blobs to include",
            out includePatterns);
    }

    public static bool TryPreview(
        this FlagConsumer consumer,
        out bool preview)
    {
        return consumer.TryBool(
            "--preview",
            "Show only a preview",
            out preview);
    }

    public static bool TryChunksOnly(
        this FlagConsumer consumer,
        out bool chunksOnly)
    {
        return consumer.TryBool(
            "--chunks-only",
            "Show chunk IDs",
            out chunksOnly);
    }
}
