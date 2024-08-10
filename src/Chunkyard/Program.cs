namespace Chunkyard;

public static class Program
{
    public static int Main(string[] args)
    {
        try
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
                .Use<HelpCommand>(Help)
                .Handle(args);
        }
        catch (Exception e)
        {
            return Error(e);
        }
    }

    private static void Check(CheckCommand c)
    {
        var valid = c.Shallow
            ? c.SnapshotStore.CheckSnapshotExists(c.SnapshotId, c.Include)
            : c.SnapshotStore.CheckSnapshotValid(c.SnapshotId, c.Include);

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
        var first = c.SnapshotStore.GetSnapshot(c.FirstSnapshotId)
            .ListBlobs(c.Include);

        var second = c.SnapshotStore.GetSnapshot(c.SecondSnapshotId)
            .ListBlobs(c.Include);

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
        var blobs = c.SnapshotStore.GetSnapshot(c.SnapshotId)
            .ListBlobs(c.Include);

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

    private static int Help(HelpCommand c)
    {
        Console.Error.WriteLine($"Chunkyard v{GetVersion()}");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  <command> <flags>");

        if (c.Infos.Any())
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("Help:");

            foreach (var info in c.Infos.OrderBy(i => i.Key))
            {
                Console.Error.WriteLine($"  {info.Key}");
                Console.Error.WriteLine($"    {info.Value}");
            }
        }

        if (c.Errors.Any())
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(c.Errors.Count == 1 ? "Error:" : "Errors:");

            foreach (var error in c.Errors.OrderBy(e => e))
            {
                Console.Error.WriteLine($"  {error}");
            }
        }

        Console.Error.WriteLine();

        return 1;
    }

    private static int Error(Exception e)
    {
        Console.Error.WriteLine("Error:");

        IEnumerable<Exception> exceptions = e is AggregateException a
            ? a.InnerExceptions
            : new[] { e };

        foreach (var exception in exceptions)
        {
            Console.Error.WriteLine(exception.ToString());
        }

        return 1;
    }

    private static string GetVersion()
    {
        var attribute = typeof(Program).Assembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute))
            .First();

        return ((AssemblyInformationalVersionAttribute)attribute)
            .InformationalVersion;
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
