namespace Chunkyard;

public sealed class CommandHandler : ICommandHandler
{
    public void Handle(CatCommand c)
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
                snapshotStore.RestoreSnapshotReference(c.SnapshotId));
        }

        if (stream is MemoryStream memoryStream)
        {
            Console.WriteLine(
                Encoding.UTF8.GetString(memoryStream.ToArray()));
        }
    }

    public void Handle(CheckCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        var fuzzy = new Fuzzy(c.IncludePatterns);

        if (c.Shallow)
        {
            snapshotStore.EnsureSnapshotExists(c.SnapshotId, fuzzy);
        }
        else
        {
            snapshotStore.EnsureSnapshotValid(c.SnapshotId, fuzzy);
        }
    }

    public void Handle(CopyCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        snapshotStore.CopyTo(
            CreateRepository(c.DestinationRepository));
    }

    public void Handle(DiffCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        var fuzzy = new Fuzzy(c.IncludePatterns);
        var first = snapshotStore.FilterSnapshot(c.FirstSnapshotId, fuzzy);
        var second = snapshotStore.FilterSnapshot(c.SecondSnapshotId, fuzzy);

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

    public void Handle(GarbageCollectCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        snapshotStore.GarbageCollect();
    }

    public void Handle(KeepCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        snapshotStore.KeepSnapshots(c.LatestCount);
    }

    public void Handle(ListCommand c)
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

    public void Handle(RemoveCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        snapshotStore.RemoveSnapshot(c.SnapshotId);
    }

    public void Handle(RestoreCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        var blobSystem = new FileBlobSystem(
            new[] { c.Directory },
            Fuzzy.Default);

        var fuzzy = new Fuzzy(c.IncludePatterns);

        if (c.Preview)
        {
            var diff = snapshotStore.RestoreSnapshotPreview(
                blobSystem,
                c.SnapshotId,
                fuzzy);

            PrintDiff(diff);
        }
        else
        {
            snapshotStore.RestoreSnapshot(
                blobSystem,
                c.SnapshotId,
                fuzzy);
        }
    }

    public void Handle(ShowCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        var blobReferences = snapshotStore.FilterSnapshot(
            c.SnapshotId,
            new Fuzzy(c.IncludePatterns));

        var contents = c.ChunksOnly
            ? blobReferences.SelectMany(br => br.ChunkIds)
            : blobReferences.Select(br => br.Blob.Name);

        foreach (var content in contents)
        {
            Console.WriteLine(content);
        }
    }

    public void Handle(StoreCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        var blobSystem = new FileBlobSystem(
            c.Paths,
            new Fuzzy(c.IncludePatterns));

        if (c.Preview)
        {
            var diff = snapshotStore.StoreSnapshotPreview(blobSystem);

            PrintDiff(diff);
        }
        else
        {
            snapshotStore.StoreSnapshot(blobSystem);
        }
    }

    public void Handle(HelpCommand c)
    {
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"  {GetType().Assembly.GetName().Name} <command> <flags>");
        Console.WriteLine();

        foreach (var usage in c.Usages)
        {
            Console.WriteLine($"  {usage.Topic}");
            Console.WriteLine($"    {usage.Info}");
            Console.WriteLine();
        }

        if (c.Errors.Any())
        {
            Console.WriteLine(c.Errors.Count == 1
                ? "Error:"
                : "Errors:");

            foreach (var error in c.Errors)
            {
                Console.WriteLine($"  {error}");
            }

            Console.WriteLine();
        }

        Environment.ExitCode = 1;
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

    private static SnapshotStore CreateSnapshotStore(Command c)
    {
        var repository = CreateRepository(c.Repository);

        IPrompt prompt = c.Prompt switch
        {
            Prompt.Console => new ConsolePrompt(),
            Prompt.Environment => new EnvironmentPrompt(),
            Prompt.Libsecret => new LibsecretPrompt(
                new ConsolePrompt(),
                repository.Id),
            _ => new ConsolePrompt()
        };

        var parallelism = c.Parallel < 1
            ? Environment.ProcessorCount
            : c.Parallel;

        return new SnapshotStore(
            repository,
            new FastCdc(),
            new ConsoleProbe(),
            new RealWorld(),
            prompt,
            parallelism);
    }

    private static IRepository CreateRepository(string repositoryPath)
    {
        return new FileRepository(repositoryPath);
    }
}

public interface ICommandHandler
{
    void Handle(CatCommand c);

    void Handle(CheckCommand c);

    void Handle(CopyCommand c);

    void Handle(DiffCommand c);

    void Handle(GarbageCollectCommand c);

    void Handle(KeepCommand c);

    void Handle(ListCommand c);

    void Handle(RemoveCommand c);

    void Handle(RestoreCommand c);

    void Handle(ShowCommand c);

    void Handle(StoreCommand c);

    void Handle(HelpCommand c);
}
