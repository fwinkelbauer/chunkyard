namespace Chunkyard;

/// <summary>
/// An <see cref="ICommand"/> that checks if a snapshot is valid.
/// </summary>
public sealed record CheckCommand(
    SnapshotStore SnapshotStore,
    int SnapshotId,
    Fuzzy Include) : ICommand
{
    public int Run()
    {
        if (!SnapshotStore.CheckSnapshot(SnapshotId, Include))
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

/// <summary>
/// An <see cref="ICommand"/> that copies snapshots from one repository to
/// another.
/// </summary>
public sealed record CopyCommand(
    SnapshotStore SnapshotStore,
    IRepository DestinationRepository) : ICommand
{
    public int Run()
    {
        SnapshotStore.CopyTo(DestinationRepository);

        return 0;
    }

    public static CopyCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryRepository("--destination", "The destination repository path", out var repository))
        {
            return new CopyCommand(snapshotStore, repository);
        }
        else
        {
            return null;
        }
    }
}

/// <summary>
/// An <see cref="ICommand"/> that shows the difference between two snapshots.
/// </summary>
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

/// <summary>
/// An <see cref="ICommand"/> that removes unreferenced chunks.
/// </summary>
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

/// <summary>
/// An <see cref="ICommand"/> that keeps only a certain amount of snapshots.
/// </summary>
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

/// <summary>
/// An <see cref="ICommand"/> that lists all snapshots.
/// </summary>
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

/// <summary>
/// An <see cref="ICommand"/> that removes a snapshot.
/// </summary>
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

/// <summary>
/// An <see cref="ICommand"/> that restores a snapshot.
/// </summary>
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
            & consumer.TryBlobSystem("--directory", "The directory to restore into", out var blobSystem)
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

/// <summary>
/// An <see cref="ICommand"/> that shows the content of a snapshot.
/// </summary>
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

/// <summary>
/// An <see cref="ICommand"/> that stores a new snapshot.
/// </summary>
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
            & consumer.TryBlobSystem("--directory", "A list of directories to store", out var blobSystem)
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
