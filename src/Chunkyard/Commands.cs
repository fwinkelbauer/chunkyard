namespace Chunkyard;

public sealed record CheckCommand(
    SnapshotStore SnapshotStore,
    int SnapshotId,
    Fuzzy Include,
    bool Shallow) : ICommand
{
    public int Run()
    {
        var valid = Shallow
            ? SnapshotStore.CheckSnapshotExists(SnapshotId, Include)
            : SnapshotStore.CheckSnapshotValid(SnapshotId, Include);

        if (!valid)
        {
            throw new ChunkyardException(
                "Snapshot contains invalid or missing chunks");
        }

        return 0;
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
            .ListBlobs(Include);

        var second = SnapshotStore.GetSnapshot(SecondSnapshotId)
            .ListBlobs(Include);

        var diff = DiffSet.Create(first, second, b => b.Name);

        foreach (var added in diff.Added)
        {
            Console.WriteLine($"+ {added.Name}");
        }

        foreach (var changed in diff.Changed)
        {
            Console.WriteLine($"~ {changed.Name}");
        }

        foreach (var removed in diff.Removed)
        {
            Console.WriteLine($"- {removed.Name}");
        }

        return 0;
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
}

public sealed record StoreCommand(
    SnapshotStore SnapshotStore,
    IBlobSystem BlobSystem,
    Fuzzy Include) : ICommand
{
    public int Run()
    {
        _ = SnapshotStore.StoreSnapshot(BlobSystem, Include);

        return 0;
    }
}
