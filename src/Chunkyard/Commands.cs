namespace Chunkyard;

public sealed class CheckCommand : ICommand
{
    public CheckCommand(
        SnapshotStore snapshotStore,
        int snapshotId,
        Fuzzy include,
        bool shallow)
    {
        SnapshotStore = snapshotStore;
        SnapshotId = snapshotId;
        Include = include;
        Shallow = shallow;
    }

    public SnapshotStore SnapshotStore { get; }

    public int SnapshotId { get; }

    public Fuzzy Include { get; }

    public bool Shallow { get; }

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

public sealed class CopyCommand : ICommand
{
    public CopyCommand(
        SnapshotStore snapshotStore,
        IRepository destinationRepository,
        int last)
    {
        SnapshotStore = snapshotStore;
        DestinationRepository = destinationRepository;
        Last = last;
    }

    public SnapshotStore SnapshotStore { get; }

    public IRepository DestinationRepository { get; }

    public int Last { get; }

    public int Run()
    {
        SnapshotStore.CopyTo(DestinationRepository, Last);

        return 0;
    }
}

public sealed class DiffCommand : ICommand
{
    public DiffCommand(
        SnapshotStore snapshotStore,
        int firstSnapshotId,
        int secondSnapshotId,
        Fuzzy include)
    {
        SnapshotStore = snapshotStore;
        FirstSnapshotId = firstSnapshotId;
        SecondSnapshotId = secondSnapshotId;
        Include = include;
    }

    public SnapshotStore SnapshotStore { get; }

    public int FirstSnapshotId { get; }

    public int SecondSnapshotId { get; }

    public Fuzzy Include { get; }

    public int Run()
    {
        var first = SnapshotStore.GetSnapshot(FirstSnapshotId)
            .ListBlobs(Include);

        var second = SnapshotStore.GetSnapshot(SecondSnapshotId)
            .ListBlobs(Include);

        var diff = DiffSet.Create(first, second, b => b.Name);
        ConsoleUtils.PrintDiff(diff);

        return 0;
    }
}

public sealed class KeepCommand : ICommand
{
    public KeepCommand(
        SnapshotStore snapshotStore,
        int latestCount)
    {
        SnapshotStore = snapshotStore;
        LatestCount = latestCount;
    }

    public SnapshotStore SnapshotStore { get; }

    public int LatestCount { get; }

    public int Run()
    {
        SnapshotStore.KeepSnapshots(LatestCount);
        SnapshotStore.GarbageCollect();

        return 0;
    }
}

public sealed class ListCommand : ICommand
{
    public ListCommand(SnapshotStore snapshotStore)
    {
        SnapshotStore = snapshotStore;
    }

    public SnapshotStore SnapshotStore { get; }

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

public sealed class RemoveCommand : ICommand
{
    public RemoveCommand(
        SnapshotStore snapshotStore,
        int snapshotId)
    {
        SnapshotStore = snapshotStore;
        SnapshotId = snapshotId;
    }

    public SnapshotStore SnapshotStore { get; }

    public int SnapshotId { get; }

    public int Run()
    {
        SnapshotStore.RemoveSnapshot(SnapshotId);
        SnapshotStore.GarbageCollect();

        return 0;
    }
}

public sealed class RestoreCommand : ICommand
{
    public RestoreCommand(
        SnapshotStore snapshotStore,
        IBlobSystem blobSystem,
        int snapshotId,
        Fuzzy include,
        bool preview)
    {
        SnapshotStore = snapshotStore;
        BlobSystem = blobSystem;
        SnapshotId = snapshotId;
        Include = include;
        Preview = preview;
    }

    public SnapshotStore SnapshotStore { get; }

    public IBlobSystem BlobSystem { get; }

    public int SnapshotId { get; }

    public Fuzzy Include { get; }

    public bool Preview { get; }

    public int Run()
    {
        if (Preview)
        {
            var diff = SnapshotStore.RestoreSnapshotPreview(
                BlobSystem,
                SnapshotId,
                Include);

            ConsoleUtils.PrintDiff(diff);
        }
        else
        {
            SnapshotStore.RestoreSnapshot(BlobSystem, SnapshotId, Include);
        }

        return 0;
    }
}

public sealed class ShowCommand : ICommand
{
    public ShowCommand(
        SnapshotStore snapshotStore,
        int snapshotId,
        Fuzzy include)
    {
        SnapshotStore = snapshotStore;
        SnapshotId = snapshotId;
        Include = include;
    }

    public SnapshotStore SnapshotStore { get; }

    public int SnapshotId { get; }

    public Fuzzy Include { get; }

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

public sealed class StoreCommand : ICommand
{
    public StoreCommand(
        SnapshotStore snapshotStore,
        IBlobSystem blobSystem,
        Fuzzy include,
        bool preview)
    {
        SnapshotStore = snapshotStore;
        BlobSystem = blobSystem;
        Include = include;
        Preview = preview;
    }

    public SnapshotStore SnapshotStore { get; }

    public IBlobSystem BlobSystem { get; }

    public Fuzzy Include { get; }

    public bool Preview { get; }

    public int Run()
    {
        if (Preview)
        {
            var diff = SnapshotStore.StoreSnapshotPreview(BlobSystem, Include);
            ConsoleUtils.PrintDiff(diff);
        }
        else
        {
            SnapshotStore.StoreSnapshot(BlobSystem, Include);
        }

        return 0;
    }
}
