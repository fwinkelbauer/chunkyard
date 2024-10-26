namespace Chunkyard;

public sealed class CheckCommandParser : ICommandParser
{
    public string Command => "check";

    public string Info => "Check if a snapshot is valid";

    public ICommand Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TrySnapshot(out var snapshotId)
            & consumer.TryInclude(out var include)
            & consumer.TryBool("--shallow", "Only check if chunks exist", out var shallow)
            & consumer.NoHelp(out var help))
        {
            return new CheckCommand(
                snapshotStore,
                snapshotId,
                include,
                shallow);
        }
        else
        {
            return help;
        }
    }
}

public sealed class CopyCommandParser : ICommandParser
{
    public string Command => "copy";

    public string Info => "Copy snapshots from one repository to another";

    public ICommand Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryString("--destination", "The destination repository path", out var destinationRepository)
            & consumer.TryInt("--last", "The maximum amount of snapshots to copy. Zero or a negative number copies all", out var last, 0)
            & consumer.NoHelp(out var help))
        {
            return new CopyCommand(
                snapshotStore,
                new FileRepository(destinationRepository),
                last);
        }
        else
        {
            return help;
        }
    }
}

public sealed class DiffCommandParser : ICommandParser
{
    public string Command => "diff";

    public string Info => "Show the difference between two snapshots";

    public ICommand Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryInt("--first", "The first snapshot ID", out var firstSnapshotId, SnapshotStore.SecondLatestSnapshotId)
            & consumer.TryInt("--second", "The second snapshot ID", out var secondSnapshotId, SnapshotStore.LatestSnapshotId)
            & consumer.TryInclude(out var include)
            & consumer.NoHelp(out var help))
        {
            return new DiffCommand(
                snapshotStore,
                firstSnapshotId,
                secondSnapshotId,
                include);
        }
        else
        {
            return help;
        }
    }
}

public sealed class GarbageCollectParser : ICommandParser
{
    public string Command => "gc";

    public string Info => "Remove unreferenced chunks";

    public ICommand Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.NoHelp(out var help))
        {
            return new GarbageCollectCommand(snapshotStore);
        }
        else
        {
            return help;
        }
    }
}

public sealed class KeepCommandParser : ICommandParser
{
    public string Command => "keep";

    public string Info => "Keep only the given list of snapshots";

    public ICommand Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryInt("--latest", "The count of the latest snapshots to keep", out var latestCount)
            & consumer.NoHelp(out var help))
        {
            return new KeepCommand(
                snapshotStore,
                latestCount);
        }
        else
        {
            return help;
        }
    }
}

public sealed class ListCommandParser : ICommandParser
{
    public string Command => "list";

    public string Info => "List all snapshots";

    public ICommand Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.NoHelp(out var help))
        {
            return new ListCommand(snapshotStore);
        }
        else
        {
            return help;
        }
    }
}

public sealed class RemoveCommandParser : ICommandParser
{
    public string Command => "remove";

    public string Info => "Remove a snapshot or a set of chunk IDs";

    public ICommand Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TrySnapshot(out var snapshot)
            & consumer.NoHelp(out var help))
        {
            return new RemoveCommand(
                snapshotStore,
                snapshot);
        }
        else
        {
            return help;
        }
    }
}

public sealed class RestoreCommandParser : ICommandParser
{
    public string Command => "restore";

    public string Info => "Restore a snapshot";

    public ICommand Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryString("--directory", "The directory to restore into", out var directory)
            & consumer.TrySnapshot(out var snapshot)
            & consumer.TryInclude(out var include)
            & consumer.TryPreview(out var preview)
            & consumer.NoHelp(out var help))
        {
            return new RestoreCommand(
                snapshotStore,
                new FileBlobSystem(directory),
                snapshot,
                include,
                preview);
        }
        else
        {
            return help;
        }
    }
}

public sealed class ShowCommandParser : ICommandParser
{
    public string Command => "show";

    public string Info => "Show the content of a snapshot";

    public ICommand Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TrySnapshot(out var snapshot)
            & consumer.TryInclude(out var include)
            & consumer.NoHelp(out var help))
        {
            return new ShowCommand(
                snapshotStore,
                snapshot,
                include);
        }
        else
        {
            return help;
        }
    }
}

public sealed class StoreCommandParser : ICommandParser
{
    public string Command => "store";

    public string Info => "Store a new snapshot";

    public ICommand Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryStrings("--path", "A list of files and directories (blobs) to store", out var paths)
            & consumer.TryInclude(out var include)
            & consumer.TryPreview(out var preview)
            & consumer.NoHelp(out var help))
        {
            return new StoreCommand(
                snapshotStore,
                new FileBlobSystem(paths),
                include,
                preview);
        }
        else
        {
            return help;
        }
    }
}

public enum Prompt
{
    Console = 0,
    Store = 1,
    Libsecret = 2
}

public static class ArgConsumerExtensions
{
    public static bool TrySnapshotStore(
        this FlagConsumer consumer,
        out SnapshotStore snapshotStore)
    {
        var success = consumer.TryString("--repository", "The repository path", out var repository)
            & consumer.TryEnum("--prompt", "The password prompt method", out Prompt promptValue, Prompt.Console)
            & consumer.TryInt("--parallel", "The degree of parallelism", out var parallel, 1);

        IPrompt prompt = promptValue switch
        {
            Prompt.Console => new ConsolePrompt(),
            Prompt.Store => new StorePrompt(new ConsolePrompt()),
            Prompt.Libsecret => new LibsecretPrompt(new ConsolePrompt()),
            _ => new ConsolePrompt()
        };

        snapshotStore = new SnapshotStore(
            new FileRepository(repository),
            new FastCdc(),
            new ConsoleProbe(),
            new RealWorld(parallel),
            prompt);

        return success;
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

    public static bool TryInclude(
        this FlagConsumer consumer,
        out Fuzzy include)
    {
        var success = consumer.TryStrings(
            "--include",
            "A list of fuzzy patterns for blobs to include",
            out var includePatterns);

        include = new Fuzzy(includePatterns);

        return success;
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
}
