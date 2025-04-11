namespace Chunkyard;

public sealed record CheckCommand(
    SnapshotStore SnapshotStore,
    int SnapshotId,
    Fuzzy Include) : ICommand
{
    public int Run()
    {
        var valid = SnapshotStore.CheckSnapshot(SnapshotId, Include);

        if (!valid)
        {
            throw new ChunkyardException(
                "Snapshot contains invalid or missing chunks");
        }

        return 0;
    }

    public static CheckCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TrySnapshot(out var snapshotId)
            & consumer.TryInclude(out var include))
        {
            return new CheckCommand(snapshotStore, snapshotId, include);
        }
        else
        {
            return null;
        }
    }
}

public sealed record CopyCommand(
    SnapshotStore SnapshotStore,
    IRepository DestinationRepository,
    int Last) : ICommand
{
    public int Run()
    {
        SnapshotStore.CopyTo(DestinationRepository, Last);

        return 0;
    }

    public static CopyCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryString("--destination", "The destination repository path", out var path)
            & consumer.TryDryRun<IRepository>(new FileRepository(path), r => new DryRunRepository(r), out var repository)
            & consumer.TryInt("--last", "The maximum amount of snapshots to copy. Zero or a negative number copies all", out var last, 0))
        {
            return new CopyCommand(snapshotStore, repository, last);
        }
        else
        {
            return null;
        }
    }
}

public sealed record DiffCommand(
    SnapshotStore SnapshotStore,
    int FirstSnapshotId,
    int SecondSnapshotId,
    Fuzzy Include) : ICommand
{
    public int Run()
    {
        var first = SnapshotStore.GetSnapshot(FirstSnapshotId)
            .ListBlobs(Include)
            .ToDictionary(b => b.Name, b => b);

        var second = SnapshotStore.GetSnapshot(SecondSnapshotId)
            .ListBlobs(Include)
            .ToDictionary(b => b.Name, b => b);

        var changes = first.Keys
            .Intersect(second.Keys)
            .Where(key => !first[key].Equals(second[key]));

        foreach (var added in second.Keys.Except(first.Keys))
        {
            Console.WriteLine($"+ {added}");
        }

        foreach (var changed in changes)
        {
            Console.WriteLine($"~ {changed}");
        }

        foreach (var removed in first.Keys.Except(second.Keys))
        {
            Console.WriteLine($"- {removed}");
        }

        return 0;
    }

    public static DiffCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryInt("--first", "The first snapshot ID", out var firstSnapshotId, SnapshotStore.SecondLatestSnapshotId)
            & consumer.TryInt("--second", "The second snapshot ID", out var secondSnapshotId, SnapshotStore.LatestSnapshotId)
            & consumer.TryInclude(out var include))
        {
            return new DiffCommand(snapshotStore, firstSnapshotId, secondSnapshotId, include);
        }
        else
        {
            return null;
        }
    }
}

public sealed record GarbageCollectCommand(
    SnapshotStore SnapshotStore) : ICommand
{
    public int Run()
    {
        SnapshotStore.GarbageCollect();

        return 0;
    }

    public static GarbageCollectCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore))
        {
            return new GarbageCollectCommand(snapshotStore);
        }
        else
        {
            return null;
        }
    }
}

public sealed record KeepCommand(
    SnapshotStore SnapshotStore,
    int LatestCount) : ICommand
{
    public int Run()
    {
        SnapshotStore.KeepSnapshots(LatestCount);

        return 0;
    }

    public static KeepCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryInt("--latest", "The count of the latest snapshots to keep", out var latestCount))
        {
            return new KeepCommand(snapshotStore, latestCount);
        }
        else
        {
            return null;
        }
    }
}

public sealed record ListCommand(
    SnapshotStore SnapshotStore) : ICommand
{
    public int Run()
    {
        foreach (var snapshotId in SnapshotStore.ListSnapshotIds())
        {
            var isoDate = SnapshotStore.GetSnapshot(snapshotId)
                .CreationTimeUtc
                .ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm:ss");

            Console.WriteLine($"Snapshot #{snapshotId}: {isoDate}");
        }

        return 0;
    }

    public static ListCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore))
        {
            return new ListCommand(snapshotStore);
        }
        else
        {
            return null;
        }
    }
}

public sealed record RemoveCommand(
    SnapshotStore SnapshotStore,
    int SnapshotId) : ICommand
{
    public int Run()
    {
        SnapshotStore.RemoveSnapshot(SnapshotId);

        return 0;
    }

    public static RemoveCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TrySnapshot(out var snapshot))
        {
            return new RemoveCommand(snapshotStore, snapshot);
        }
        else
        {
            return null;
        }
    }
}

public sealed record RestoreCommand(
    SnapshotStore SnapshotStore,
    IBlobSystem BlobSystem,
    int SnapshotId,
    Fuzzy Include) : ICommand
{
    public int Run()
    {
        SnapshotStore.RestoreSnapshot(BlobSystem, SnapshotId, Include);

        return 0;
    }

    public static RestoreCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryString("--directory", "The directory to restore into", out var directory)
            & consumer.TryDryRun<IBlobSystem>(new FileBlobSystem(directory), b => new DryRunBlobSystem(b), out var blobSystem)
            & consumer.TrySnapshot(out var snapshot)
            & consumer.TryInclude(out var include))
        {
            return new RestoreCommand(snapshotStore, blobSystem, snapshot, include);
        }
        else
        {
            return null;
        }
    }
}

public sealed record ShowCommand(
    SnapshotStore SnapshotStore,
    int SnapshotId,
    Fuzzy Include) : ICommand
{
    public int Run()
    {
        var blobs = SnapshotStore.GetSnapshot(SnapshotId)
            .ListBlobs(Include);

        foreach (var blob in blobs)
        {
            Console.WriteLine(blob.Name);
        }

        return 0;
    }

    public static ShowCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TrySnapshot(out var snapshot)
            & consumer.TryInclude(out var include))
        {
            return new ShowCommand(snapshotStore, snapshot, include);
        }
        else
        {
            return null;
        }
    }
}

public sealed record StoreCommand(
    SnapshotStore SnapshotStore,
    IBlobSystem BlobSystem,
    Fuzzy Include) : ICommand
{
    public int Run()
    {
        _ = SnapshotStore.StoreSnapshot(BlobSystem, DateTime.UtcNow, Include);

        return 0;
    }

    public static StoreCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryStrings("--directory", "A list of directories to store", out var directories)
            & consumer.TryDryRun<IBlobSystem>(new FileBlobSystem(directories), b => new DryRunBlobSystem(b), out var blobSystem)
            & consumer.TryInclude(out var include))
        {
            return new StoreCommand(snapshotStore, blobSystem, include);
        }
        else
        {
            return null;
        }
    }
}

public enum Password
{
    Console = 0,
    Libsecret = 1
}

public static class ArgConsumerExtensions
{
    public static bool TrySnapshotStore(
        this FlagConsumer consumer,
        out SnapshotStore snapshotStore)
    {
        var success = consumer.TryString("--repository", "The repository path", out var path)
            & consumer.TryEnum("--password", "The password prompt method", out Password password, Password.Console)
            & consumer.TryDryRun<IRepository>(new FileRepository(path), r => new DryRunRepository(r), out var repository);

        ICryptoFactory cryptoFactory = password switch
        {
            Password.Console => new ConsoleCryptoFactory(),
            Password.Libsecret => new LibsecretCryptoFactory(new ConsoleCryptoFactory()),
            _ => new ConsoleCryptoFactory()
        };

        snapshotStore = new SnapshotStore(
            repository,
            new SimpleChunker(),
            new ConsoleProbe(),
            cryptoFactory);

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
            "A list of fuzzy patterns for files to include",
            out var includePatterns,
            Array.Empty<string>());

        include = success
            ? new Fuzzy(includePatterns)
            : new Fuzzy();

        return success;
    }

    public static bool TryDryRun<T>(
        this FlagConsumer consumer,
        T input,
        Func<T, T> decorator,
        out T output)
    {
        var success = consumer.TryBool(
            "--dry-run",
            "Do not persist any data changes",
            out var dryRun);

        output = dryRun ? decorator(input) : input;

        return success;
    }
}
