namespace Chunkyard;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CommandHandler()
            .With<CheckCommand>(new CheckCommandParser(), Check)
            .With<CopyCommand>(new CopyCommandParser(), Copy)
            .With<DiffCommand>(new DiffCommandParser(), Diff)
            .With<KeepCommand>(new KeepCommandParser(), Keep)
            .With<ListCommand>(new ListCommandParser(), List)
            .With<RemoveCommand>(new RemoveCommandParser(), Remove)
            .With<RestoreCommand>(new RestoreCommandParser(), Restore)
            .With<ShowCommand>(new ShowCommandParser(), Show)
            .With<StoreCommand>(new StoreCommandParser(), Store)
            .Handle(args);
    }

    private static void Check(CheckCommand c)
    {
        var valid = true;

        if (c.Shallow)
        {
            valid = c.SnapshotStore.CheckSnapshotExists(c.SnapshotId, c.Include);
        }
        else
        {
            valid = c.SnapshotStore.CheckSnapshotValid(c.SnapshotId, c.Include);
        }

        if (!valid)
        {
            throw new ChunkyardException(
                "Snapshot contains invalid or missing chunks");
        }
    }

    private static void Copy(CopyCommand c)
    {
        c.SnapshotStore.CopyTo(
            c.DestinationRepository,
            c.Last);
    }

    private static void Diff(DiffCommand c)
    {
        var first = c.SnapshotStore.ListBlobs(c.FirstSnapshotId, c.Include);
        var second = c.SnapshotStore.ListBlobs(c.SecondSnapshotId, c.Include);
        var diff = DiffSet.Create(first, second, b => b.Name);

        PrintDiff(diff);
    }

    private static void Keep(KeepCommand c)
    {
        c.SnapshotStore.KeepSnapshots(c.LatestCount);
        c.SnapshotStore.GarbageCollect();
    }

    private static void List(ListCommand c)
    {
        foreach (var snapshotId in c.SnapshotStore.ListSnapshotIds())
        {
            var isoDate = c.SnapshotStore.GetSnapshot(snapshotId)
                .CreationTimeUtc
                .ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm:ss");

            Console.WriteLine($"Snapshot #{snapshotId}: {isoDate}");
        }
    }

    private static void Remove(RemoveCommand c)
    {
        c.SnapshotStore.RemoveSnapshot(c.SnapshotId);
        c.SnapshotStore.GarbageCollect();
    }

    private static void Restore(RestoreCommand c)
    {
        if (c.Preview)
        {
            var diff = c.SnapshotStore.RestoreSnapshotPreview(
                c.BlobSystem,
                c.SnapshotId,
                c.Include);

            PrintDiff(diff);
        }
        else
        {
            c.SnapshotStore.RestoreSnapshot(
                c.BlobSystem,
                c.SnapshotId,
                c.Include);
        }
    }

    private static void Show(ShowCommand c)
    {
        var blobs = c.SnapshotStore.ListBlobs(c.SnapshotId, c.Include);

        foreach (var blob in blobs)
        {
            Console.WriteLine(blob.Name);
        }
    }

    private static void Store(StoreCommand c)
    {
        if (c.Preview)
        {
            var diff = c.SnapshotStore.StoreSnapshotPreview(
                c.BlobSystem,
                c.Include);

            PrintDiff(diff);
        }
        else
        {
            c.SnapshotStore.StoreSnapshot(
                c.BlobSystem,
                c.Include);
        }
    }

    private static void PrintDiff(DiffSet<Blob> diff)
    {
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
    }
}
