namespace Chunkyard;

public sealed class CatCommandParser : ICommandParser
{
    public string Command => "cat";

    public string Info => "Export or print the value of a snapshot or a set of chunk IDs";

    public ICommand Parse(ArgConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & (consumer.TrySnapshot(out var snapshotId)
                | consumer.TryList("--chunks", "The chunk IDs", out var chunkIds))
            & consumer.TryString("--export", "The export path", out var export, "")
            & consumer.IsConsumed())
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
            return new HelpCommand(consumer.HelpTexts, consumer.Errors);
        }
    }
}

public sealed class CheckCommandParser : ICommandParser
{
    public string Command => "check";

    public string Info => "Check if a snapshot is valid";

    public ICommand Parse(ArgConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TrySnapshot(out var snapshotId)
            & consumer.TryIncludePatterns(out var includePatterns)
            & consumer.TryBool("--shallow", "Only check if chunks exist", out var shallow)
            & consumer.IsConsumed())
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
            return new HelpCommand(consumer.HelpTexts, consumer.Errors);
        }
    }
}

public sealed class CopyCommandParser : ICommandParser
{
    public string Command => "copy";

    public string Info => "Copy snapshots from one repository to another";

    public ICommand Parse(ArgConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TryString("--destination", "The destination repository path", out var destinationRepository)
            & consumer.IsConsumed())
        {
            return new CopyCommand(
                repository,
                prompt,
                parallel,
                destinationRepository);
        }
        else
        {
            return new HelpCommand(consumer.HelpTexts, consumer.Errors);
        }
    }
}

public sealed class DiffCommandParser : ICommandParser
{
    public string Command => "diff";

    public string Info => "Show the difference between two snapshots";

    public ICommand Parse(ArgConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TryInt("--first", "The first snapshot ID", out var firstSnapshotId, SnapshotStore.SecondLatestSnapshotId)
            & consumer.TryInt("--second", "The second snapshot ID", out var secondSnapshotId, SnapshotStore.LatestSnapshotId)
            & consumer.TryIncludePatterns(out var includePatterns)
            & consumer.TryChunksOnly(out var chunksOnly)
            & consumer.IsConsumed())
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
            return new HelpCommand(consumer.HelpTexts, consumer.Errors);
        }
    }
}

public sealed class GarbageCollectCommandParser : ICommandParser
{
    public string Command => "gc";

    public string Info => "Remove unreferenced chunks";

    public ICommand Parse(ArgConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.IsConsumed())
        {
            return new GarbageCollectCommand(
                repository,
                prompt,
                parallel);
        }
        else
        {
            return new HelpCommand(consumer.HelpTexts, consumer.Errors);
        }
    }
}

public sealed class KeepCommandParser : ICommandParser
{
    public string Command => "keep";

    public string Info => "Keep only the given list of snapshots";

    public ICommand Parse(ArgConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TryInt("--latest", "The count of the latest snapshots to keep", out var latestCount)
            & consumer.IsConsumed())
        {
            return new KeepCommand(
                repository,
                prompt,
                parallel,
                latestCount);
        }
        else
        {
            return new HelpCommand(consumer.HelpTexts, consumer.Errors);
        }
    }
}

public sealed class ListCommandParser : ICommandParser
{
    public string Command => "list";

    public string Info => "List all snapshots";

    public ICommand Parse(ArgConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.IsConsumed())
        {
            return new ListCommand(
                repository,
                prompt,
                parallel);
        }
        else
        {
            return new HelpCommand(consumer.HelpTexts, consumer.Errors);
        }
    }
}

public sealed class RemoveCommandParser : ICommandParser
{
    public string Command => "remove";

    public string Info => "Remove a snapshot or a set of chunk IDs";

    public ICommand Parse(ArgConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TrySnapshot(out var snapshot)
            & consumer.IsConsumed())
        {
            return new RemoveCommand(
                repository,
                prompt,
                parallel,
                snapshot);
        }
        else
        {
            return new HelpCommand(consumer.HelpTexts, consumer.Errors);
        }
    }
}

public sealed class RestoreCommandParser : ICommandParser
{
    public string Command => "restore";

    public string Info => "Restore a snapshot";

    public ICommand Parse(ArgConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TryString("--directory", "The directory to restore into", out var directory)
            & consumer.TrySnapshot(out var snapshot)
            & consumer.TryIncludePatterns(out var includePatterns)
            & consumer.TryPreview(out var preview)
            & consumer.IsConsumed())
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
            return new HelpCommand(consumer.HelpTexts, consumer.Errors);
        }
    }
}

public sealed class ShowCommandParser : ICommandParser
{
    public string Command => "show";

    public string Info => "Show the content of a snapshot";

    public ICommand Parse(ArgConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TrySnapshot(out var snapshot)
            & consumer.TryIncludePatterns(out var includePatterns)
            & consumer.TryChunksOnly(out var chunksOnly)
            & consumer.IsConsumed())
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
            return new HelpCommand(consumer.HelpTexts, consumer.Errors);
        }
    }
}

public sealed class StoreCommandParser : ICommandParser
{
    public string Command => "store";

    public string Info => "Store a new snapshot";

    public ICommand Parse(ArgConsumer consumer)
    {
        if (consumer.TryCommon(out var repository, out var prompt, out var parallel)
            & consumer.TryList("--paths", "The files and directories (blobs) to store", out var paths)
            & consumer.TryIncludePatterns(out var includePatterns)
            & consumer.TryPreview(out var preview)
            & consumer.IsConsumed())
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
            return new HelpCommand(consumer.HelpTexts, consumer.Errors);
        }
    }
}

public interface IChunkyardCommand : ICommand
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

    public void Run()
    {
        var snapshotStore = CommandUtils.CreateSnapshotStore(this);

        using Stream stream = string.IsNullOrEmpty(Export)
            ? new MemoryStream()
            : new FileStream(Export, FileMode.CreateNew, FileAccess.Write);

        if (ChunkIds.Any())
        {
            snapshotStore.RestoreChunks(ChunkIds, stream);
        }
        else
        {
            stream.Write(
                snapshotStore.RestoreSnapshotReference(SnapshotId));
        }

        if (stream is MemoryStream memoryStream)
        {
            Console.WriteLine(
                Encoding.UTF8.GetString(memoryStream.ToArray()));
        }
    }
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

    public void Run()
    {
        var snapshotStore = CommandUtils.CreateSnapshotStore(this);

        var fuzzy = new Fuzzy(IncludePatterns);

        if (Shallow)
        {
            snapshotStore.EnsureSnapshotExists(SnapshotId, fuzzy);
        }
        else
        {
            snapshotStore.EnsureSnapshotValid(SnapshotId, fuzzy);
        }
    }
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

    public void Run()
    {
        var snapshotStore = CommandUtils.CreateSnapshotStore(this);

        snapshotStore.CopyTo(
            CommandUtils.CreateRepository(DestinationRepository));
    }
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

    public void Run()
    {
        var snapshotStore = CommandUtils.CreateSnapshotStore(this);

        var fuzzy = new Fuzzy(IncludePatterns);
        var first = snapshotStore.FilterSnapshot(FirstSnapshotId, fuzzy);
        var second = snapshotStore.FilterSnapshot(SecondSnapshotId, fuzzy);

        var diff = ChunksOnly
            ? DiffSet.Create(
                first.SelectMany(br => br.ChunkIds),
                second.SelectMany(br => br.ChunkIds),
                chunkId => chunkId)
            : DiffSet.Create(
                first,
                second,
                br => br.Blob.Name);

        CommandUtils.PrintDiff(diff);
    }
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

    public void Run()
    {
        var snapshotStore = CommandUtils.CreateSnapshotStore(this);

        snapshotStore.GarbageCollect();
    }
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

    public void Run()
    {
        var snapshotStore = CommandUtils.CreateSnapshotStore(this);

        snapshotStore.KeepSnapshots(LatestCount);
    }
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

    public void Run()
    {
        var snapshotStore = CommandUtils.CreateSnapshotStore(this);

        foreach (var snapshotId in snapshotStore.ListSnapshotIds())
        {
            var isoDate = snapshotStore.GetSnapshot(snapshotId).CreationTimeUtc
                .ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm:ss");

            Console.WriteLine(
                $"Snapshot #{snapshotId}: {isoDate}");
        }
    }
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

    public void Run()
    {
        var snapshotStore = CommandUtils.CreateSnapshotStore(this);

        snapshotStore.RemoveSnapshot(SnapshotId);
    }
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

    public void Run()
    {
        var snapshotStore = CommandUtils.CreateSnapshotStore(this);

        var blobSystem = new FileBlobSystem(
            new[] { Directory },
            Fuzzy.Default);

        var fuzzy = new Fuzzy(IncludePatterns);

        if (Preview)
        {
            var diff = snapshotStore.RestoreSnapshotPreview(
                blobSystem,
                SnapshotId,
                fuzzy);

            CommandUtils.PrintDiff(diff);
        }
        else
        {
            snapshotStore.RestoreSnapshot(
                blobSystem,
                SnapshotId,
                fuzzy);
        }
    }
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

    public void Run()
    {
        var snapshotStore = CommandUtils.CreateSnapshotStore(this);

        var blobReferences = snapshotStore.FilterSnapshot(
            SnapshotId,
            new Fuzzy(IncludePatterns));

        var contents = ChunksOnly
            ? blobReferences.SelectMany(br => br.ChunkIds)
            : blobReferences.Select(br => br.Blob.Name);

        foreach (var content in contents)
        {
            Console.WriteLine(content);
        }
    }
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

    public void Run()
    {
        var snapshotStore = CommandUtils.CreateSnapshotStore(this);

        var blobSystem = new FileBlobSystem(
            Paths,
            new Fuzzy(IncludePatterns));

        if (Preview)
        {
            var diff = snapshotStore.StoreSnapshotPreview(blobSystem);

            CommandUtils.PrintDiff(diff);
        }
        else
        {
            snapshotStore.StoreSnapshot(blobSystem);
        }
    }
}

public static class CommandUtils
{
    public static void PrintDiff(DiffSet diff)
    {
        foreach (var added in diff.Added)
        {
            Console.WriteLine($"+ {added}");
        }

        foreach (var changed in diff.Changed)
        {
            Console.WriteLine($"~ {changed}");
        }

        foreach (var removed in diff.Removed)
        {
            Console.WriteLine($"- {removed}");
        }
    }

    public static SnapshotStore CreateSnapshotStore(IChunkyardCommand c)
    {
        var repository = CreateRepository(c.Repository);

        IPrompt prompt = c.Prompt switch
        {
            Prompt.Console => new ConsolePrompt(),
            Prompt.Environment => new EnvironmentPrompt(),
            Prompt.Libsecret => new LibsecretPrompt(
                new ConsolePrompt(),
                repository.Id),
            _ => new ConsolePrompt()
        };

        var parallelism = c.Parallel < 1
            ? Environment.ProcessorCount
            : c.Parallel;

        return new SnapshotStore(
            repository,
            new FastCdc(),
            new ConsoleProbe(),
            new RealWorld(),
            prompt,
            parallelism);
    }

    public static IRepository CreateRepository(string repositoryPath)
    {
        return new FileRepository(repositoryPath);
    }
}

public static class ArgConsumerExtensions
{
    public static bool TryCommon(
        this ArgConsumer consumer,
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
        this ArgConsumer consumer,
        out int snapshot)
    {
        return consumer.TryInt(
            "--snapshot",
            "The snapshot ID",
            out snapshot,
            SnapshotStore.LatestSnapshotId);
    }

    public static bool TryIncludePatterns(
        this ArgConsumer consumer,
        out IReadOnlyCollection<string> includePatterns)
    {
        return consumer.TryList(
            "--include",
            "The fuzzy patterns for blobs to include",
            out includePatterns);
    }

    public static bool TryPreview(
        this ArgConsumer consumer,
        out bool preview)
    {
        return consumer.TryBool(
            "--preview",
            "Show only a preview",
            out preview);
    }

    public static bool TryChunksOnly(
        this ArgConsumer consumer,
        out bool chunksOnly)
    {
        return consumer.TryBool(
            "--chunks-only",
            "Show chunk IDs",
            out chunksOnly);
    }
}
