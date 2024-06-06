namespace Chunkyard;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CommandHandler()
            .With<CatCommand>(new CatCommandParser(), Cat)
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

    private static void Cat(CatCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        using Stream stream = string.IsNullOrEmpty(c.Export)
            ? new MemoryStream()
            : new FileStream(c.Export, FileMode.CreateNew, FileAccess.Write);

        if (c.ChunkIds.Any())
        {
            snapshotStore.RestoreChunks(c.ChunkIds, stream);
        }
        else
        {
            stream.Write(
                Serialize.SnapshotReferenceToBytes(
                    snapshotStore.GetSnapshotReference(c.SnapshotId)));
        }

        if (stream is MemoryStream memoryStream)
        {
            Console.WriteLine(
                Encoding.UTF8.GetString(memoryStream.ToArray()));
        }
    }

    private static void Check(CheckCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        if (c.Shallow)
        {
            snapshotStore.EnsureSnapshotExists(c.SnapshotId, c.Include);
        }
        else
        {
            snapshotStore.EnsureSnapshotValid(c.SnapshotId, c.Include);
        }
    }

    private static void Copy(CopyCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        snapshotStore.CopyTo(
            CreateRepository(c.DestinationRepository),
            c.Last);
    }

    private static void Diff(DiffCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        var first = snapshotStore.FilterSnapshot(c.FirstSnapshotId, c.Include);
        var second = snapshotStore.FilterSnapshot(c.SecondSnapshotId, c.Include);

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
        var snapshotStore = CreateSnapshotStore(c);

        snapshotStore.KeepSnapshots(c.LatestCount);
        snapshotStore.GarbageCollect();
    }

    private static void List(ListCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        foreach (var snapshotId in snapshotStore.ListSnapshotIds())
        {
            var isoDate = snapshotStore.GetSnapshot(snapshotId).CreationTimeUtc
                .ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm:ss");

            Console.WriteLine(
                $"Snapshot #{snapshotId}: {isoDate}");
        }
    }

    private static void Remove(RemoveCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        snapshotStore.RemoveSnapshot(c.SnapshotId);
    }

    private static void Restore(RestoreCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);
        var blobSystem = new FileBlobSystem(c.Directory);

        if (c.Preview)
        {
            var diff = snapshotStore.RestoreSnapshotPreview(
                blobSystem,
                c.SnapshotId,
                c.Include);

            PrintDiff(diff);
        }
        else
        {
            snapshotStore.RestoreSnapshot(
                blobSystem,
                c.SnapshotId,
                c.Include);
        }
    }

    private static void Show(ShowCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        var blobReferences = snapshotStore.FilterSnapshot(
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
        var snapshotStore = CreateSnapshotStore(c);
        var blobSystem = new FileBlobSystem(c.Paths);

        if (c.Preview)
        {
            var diff = snapshotStore.StoreSnapshotPreview(blobSystem, c.Include);

            PrintDiff(diff);
        }
        else
        {
            snapshotStore.StoreSnapshot(blobSystem, c.Include);
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

    private static SnapshotStore CreateSnapshotStore(IChunkyardCommand c)
    {
        var repository = CreateRepository(c.Repository);

        IPrompt prompt = c.Prompt switch
        {
            Prompt.Console => new ConsolePrompt(),
            Prompt.Store => new StorePrompt(new ConsolePrompt()),
            _ => new ConsolePrompt()
        };

        return new SnapshotStore(
            repository,
            new FastCdc(),
            new ConsoleProbe(),
            new RealWorld(c.Parallel),
            prompt);
    }

    private static FileRepository CreateRepository(string repositoryPath)
    {
        return new FileRepository(repositoryPath);
    }
}
