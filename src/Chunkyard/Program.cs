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
            .With<VersionCommand>(new VersionCommandParser(), Version)
            .Handle(args);
    }

    private static void Check(CheckCommand c)
    {
        if (c.Shallow)
        {
            c.SnapshotStore.EnsureSnapshotExists(c.SnapshotId, c.Include);
        }
        else
        {
            c.SnapshotStore.EnsureSnapshotValid(c.SnapshotId, c.Include);
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
        var first = c.SnapshotStore.FilterSnapshot(c.FirstSnapshotId, c.Include);
        var second = c.SnapshotStore.FilterSnapshot(c.SecondSnapshotId, c.Include);

        var diff = c.ChunksOnly
            ? DiffSet.Create(
                first.SelectMany(br => br.ChunkIds),
                second.SelectMany(br => br.ChunkIds),
                chunkId => chunkId)
            : DiffSet.Create(
                first,
                second,
                br => br.Blob.Name);

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
        var blobReferences = c.SnapshotStore.FilterSnapshot(
            c.SnapshotId,
            c.Include);

        var contents = c.ChunksOnly
            ? blobReferences.SelectMany(br => br.ChunkIds)
            : blobReferences.Select(br => br.Blob.Name);

        foreach (var content in contents)
        {
            Console.WriteLine(content);
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

    private static void Version(VersionCommand c)
    {
        Console.WriteLine(c.Version);
    }

    private static void PrintDiff(DiffSet diff)
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
}
