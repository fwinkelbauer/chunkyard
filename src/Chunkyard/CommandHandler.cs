namespace Chunkyard;

/// <summary>
/// Handles every available command line verb of the Chunkyard assembly.
/// </summary>
internal static class CommandHandler
{
    public static void Cat(CatCommand c)
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

    public static void CheckSnapshot(CheckCommand c)
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

    public static void Copy(CopyCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        snapshotStore.CopyTo(
            CreateRepository(c.DestinationRepository));
    }

    public static void DiffSnapshots(DiffCommand c)
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

    public static void GarbageCollect(GarbageCollectCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        snapshotStore.GarbageCollect();
    }

    public static void KeepSnapshots(KeepCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        snapshotStore.KeepSnapshots(c.LatestCount);
    }

    public static void ListSnapshots(ListCommand c)
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

    public static void RemoveSnapshot(RemoveCommand c)
    {
        var snapshotStore = CreateSnapshotStore(c);

        snapshotStore.RemoveSnapshot(c.SnapshotId);
    }

    public static void RestoreSnapshot(RestoreCommand c)
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

    public static void ShowSnapshot(ShowCommand c)
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

    public static void StoreSnapshot(StoreCommand c)
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
