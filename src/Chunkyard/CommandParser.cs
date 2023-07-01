namespace Chunkyard;

public static class CommandParser
{
    public static IReadOnlyCollection<Usage> Usages
        => Parsers.Select(p => new Usage(p.Command, p.Info)).ToArray();

    public static IReadOnlyCollection<ICommandParser> Parsers => new ICommandParser[]
    {
        new CatCommandParser(),
        new CheckCommandParser(),
        new CopyCommandParser(),
        new DiffCommandParser(),
        new GarbageCollectCommandParser(),
        new HelpCommandParser(),
        new KeepCommandParser(),
        new ListCommandParser(),
        new RemoveCommandParser(),
        new RestoreCommandParser(),
        new ShowCommandParser(),
        new StoreCommandParser()
    };

    public static Command Parse(params string[] args)
    {
        var argResult = Arg.Parse(args);

        if (argResult.Value == null)
        {
            return new HelpCommand(Usages, argResult.Errors);
        }

        var parser = Parsers.FirstOrDefault(
            p => p.Command.Equals(argResult.Value.Command));

        return parser == null
            ? new HelpCommand(
                Usages,
                new[] { $"Unknown command: {argResult.Value.Command}" })
            : parser.Parse(argResult.Value);
    }
}

public interface ICommandParser
{
    string Command { get; }

    string Info { get; }

    Command Parse(Arg arg);
}

public sealed class CatCommandParser : ICommandParser
{
    public string Command => "cat";

    public string Info => "Export or print the value of a snapshot or a set of chunk IDs";

    public Command Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        if (consumer.TryRepository(out var repository)
            & consumer.TryPrompt(out var prompt)
            & (consumer.TrySnapshot(out var snapshotId)
                | consumer.TryList("--chunks", "The chunk IDs", out var chunkIds))
            & consumer.TryString("--export", "The export path", out var export, "")
            & consumer.IsConsumed())
        {
            return new CatCommand(
                repository,
                prompt,
                snapshotId,
                chunkIds,
                export);
        }
        else
        {
            return new HelpCommand(consumer.Usages, consumer.Errors);
        }
    }
}

public sealed class CheckCommandParser : ICommandParser
{
    public string Command => "check";

    public string Info => "Check if a snapshot is valid";

    public Command Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        if (consumer.TryRepository(out var repository)
            & consumer.TryPrompt(out var prompt)
            & consumer.TryParallel(out var parallel)
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
            return new HelpCommand(consumer.Usages, consumer.Errors);
        }
    }
}

public sealed class CopyCommandParser : ICommandParser
{
    public string Command => "copy";

    public string Info => "Copy snapshots from one repository to another";

    public Command Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        if (consumer.TryRepository(out var repository)
            & consumer.TryPrompt(out var prompt)
            & consumer.TryParallel(out var parallel)
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
            return new HelpCommand(consumer.Usages, consumer.Errors);
        }
    }
}

public sealed class DiffCommandParser : ICommandParser
{
    public string Command => "diff";

    public string Info => "Show the difference between two snapshots";

    public Command Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        if (consumer.TryRepository(out var repository)
            & consumer.TryPrompt(out var prompt)
            & consumer.TryInt("--first", "The first snapshot ID", out var firstSnapshotId, SnapshotStore.SecondLatestSnapshotId)
            & consumer.TryInt("--second", "The second snapshot ID", out var secondSnapshotId, SnapshotStore.LatestSnapshotId)
            & consumer.TryIncludePatterns(out var includePatterns)
            & consumer.TryChunksOnly(out var chunksOnly)
            & consumer.IsConsumed())
        {
            return new DiffCommand(
                repository,
                prompt,
                firstSnapshotId,
                secondSnapshotId,
                includePatterns,
                chunksOnly);
        }
        else
        {
            return new HelpCommand(consumer.Usages, consumer.Errors);
        }
    }
}

public sealed class GarbageCollectCommandParser : ICommandParser
{
    public string Command => "gc";

    public string Info => "Remove unreferenced chunks";

    public Command Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        if (consumer.TryRepository(out var repository)
            & consumer.TryPrompt(out var prompt)
            & consumer.IsConsumed())
        {
            return new GarbageCollectCommand(
                repository,
                prompt);
        }
        else
        {
            return new HelpCommand(consumer.Usages, consumer.Errors);
        }
    }
}

public sealed class HelpCommandParser : ICommandParser
{
    public string Command => "help";

    public string Info => "Print usage information";

    public Command Parse(Arg arg)
        => new HelpCommand(CommandParser.Usages, Array.Empty<string>());
}

public sealed class KeepCommandParser : ICommandParser
{
    public string Command => "keep";

    public string Info => "Keep only the given list of snapshots";

    public Command Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        if (consumer.TryRepository(out var repository)
            & consumer.TryPrompt(out var prompt)
            & consumer.TryInt("--latest", "The count of the latest snapshots to keep", out var latestCount)
            & consumer.IsConsumed())
        {
            return new KeepCommand(
                repository,
                prompt,
                latestCount);
        }
        else
        {
            return new HelpCommand(consumer.Usages, consumer.Errors);
        }
    }
}

public sealed class ListCommandParser : ICommandParser
{
    public string Command => "list";

    public string Info => "List all snapshots";

    public Command Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        if (consumer.TryRepository(out var repository)
            & consumer.TryPrompt(out var prompt)
            & consumer.IsConsumed())
        {
            return new ListCommand(
                repository,
                prompt);
        }
        else
        {
            return new HelpCommand(consumer.Usages, consumer.Errors);
        }
    }
}

public sealed class RemoveCommandParser : ICommandParser
{
    public string Command => "remove";

    public string Info => "Remove a snapshot or a set of chunk IDs";

    public Command Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        if (consumer.TryRepository(out var repository)
            & consumer.TryPrompt(out var prompt)
            & consumer.TrySnapshot(out var snapshot)
            & consumer.IsConsumed())
        {
            return new RemoveCommand(
                repository,
                prompt,
                snapshot);
        }
        else
        {
            return new HelpCommand(consumer.Usages, consumer.Errors);
        }
    }
}

public sealed class RestoreCommandParser : ICommandParser
{
    public string Command => "restore";

    public string Info => "Restore a snapshot";

    public Command Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        if (consumer.TryRepository(out var repository)
            & consumer.TryPrompt(out var prompt)
            & consumer.TryParallel(out var parallel)
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
            return new HelpCommand(consumer.Usages, consumer.Errors);
        }
    }
}

public sealed class ShowCommandParser : ICommandParser
{
    public string Command => "show";

    public string Info => "Show the content of a snapshot";

    public Command Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        if (consumer.TryRepository(out var repository)
            & consumer.TryPrompt(out var prompt)
            & consumer.TrySnapshot(out var snapshot)
            & consumer.TryIncludePatterns(out var includePatterns)
            & consumer.TryChunksOnly(out var chunksOnly)
            & consumer.IsConsumed())
        {
            return new ShowCommand(
                repository,
                prompt,
                snapshot,
                includePatterns,
                chunksOnly);
        }
        else
        {
            return new HelpCommand(consumer.Usages, consumer.Errors);
        }
    }
}

public sealed class StoreCommandParser : ICommandParser
{
    public string Command => "store";

    public string Info => "Store a new snapshot";

    public Command Parse(Arg arg)
    {
        var consumer = new ArgConsumer(arg);

        if (consumer.TryRepository(out var repository)
            & consumer.TryParallel(out var parallel)
            & consumer.TryPrompt(out var prompt)
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
            return new HelpCommand(consumer.Usages, consumer.Errors);
        }
    }
}
