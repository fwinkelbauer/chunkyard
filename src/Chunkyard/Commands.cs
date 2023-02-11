namespace Chunkyard;

/// <summary>
/// Describes every available command line verb of the Chunkyard assembly.
/// </summary>
internal static class Commands
{
    public const string DefaultRepository = ".chunkyard";

    public static void StoreSnapshot(StoreOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.Repository);

        var blobSystem = new FileBlobSystem(
            o.Paths,
            new Fuzzy(o.IncludePatterns));

        if (o.Preview)
        {
            var diffSet = snapshotStore.StoreSnapshotPreview(blobSystem);

            PrintDiff(diffSet);
        }
        else
        {
            snapshotStore.StoreSnapshot(blobSystem);
        }
    }

    public static void CheckSnapshot(CheckOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.Repository);

        var fuzzy = new Fuzzy(o.IncludePatterns);

        var ok = o.Shallow
            ? snapshotStore.CheckSnapshotExists(
                o.SnapshotId,
                fuzzy)
            : snapshotStore.CheckSnapshotValid(
                o.SnapshotId,
                fuzzy);

        if (!ok)
        {
            throw new ChunkyardException(
                "Found errors while checking snapshot");
        }
    }

    public static void ShowSnapshot(ShowOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.Repository);

        var blobReferences = snapshotStore.FilterSnapshot(
            o.SnapshotId,
            new Fuzzy(o.IncludePatterns));

        var contents = o.ChunksOnly
            ? blobReferences.SelectMany(br => br.ChunkIds)
            : blobReferences.Select(br => br.Blob.Name);

        foreach (var content in contents)
        {
            Console.WriteLine(content);
        }
    }

    public static void RestoreSnapshot(RestoreOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.Repository);

        var blobSystem = new FileBlobSystem(
            new[] { o.Directory },
            Fuzzy.Default);

        var fuzzy = new Fuzzy(o.IncludePatterns);

        if (o.Preview)
        {
            var diffSet = snapshotStore.RestoreSnapshotPreview(
                blobSystem,
                o.SnapshotId,
                fuzzy);

            PrintDiff(diffSet);
        }
        else
        {
            snapshotStore.RestoreSnapshot(
                blobSystem,
                o.SnapshotId,
                fuzzy);
        }
    }

    public static void ListSnapshots(ListOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.Repository);

        foreach (var snapshotId in snapshotStore.ListSnapshotIds())
        {
            var snapshot = snapshotStore.GetSnapshot(snapshotId);
            var isoDate = snapshot.CreationTimeUtc
                .ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm:ss");

            Console.WriteLine(
                $"Snapshot #{snapshotId}: {isoDate}");
        }
    }

    public static void DiffSnapshots(DiffOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.Repository);

        var fuzzy = new Fuzzy(o.IncludePatterns);
        var first = snapshotStore.FilterSnapshot(o.FirstSnapshotId, fuzzy);
        var second = snapshotStore.FilterSnapshot(o.SecondSnapshotId, fuzzy);

        var diff = o.ChunksOnly
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

    public static void RemoveSnapshot(RemoveOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.Repository);

        snapshotStore.RemoveSnapshot(o.SnapshotId);
    }

    public static void KeepSnapshots(KeepOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.Repository);

        snapshotStore.KeepSnapshots(o.LatestCount);
    }

    public static void GarbageCollect(GarbageCollectOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.Repository);

        snapshotStore.GarbageCollect();
    }

    public static void Copy(CopyOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.SourceRepository);
        var otherRepository = CreateRepository(o.DestinationRepository);

        snapshotStore.CopyTo(otherRepository);
    }

    public static void Cat(CatOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.Repository);

        using Stream stream = string.IsNullOrEmpty(o.Export)
            ? new MemoryStream()
            : new FileStream(o.Export, FileMode.CreateNew, FileAccess.Write);

        if (o.ChunkIds.Any())
        {
            snapshotStore.RestoreChunks(o.ChunkIds, stream);
        }
        else
        {
            stream.Write(
                snapshotStore.RestoreSnapshotReference(o.SnapshotId));
        }

        if (stream is MemoryStream memoryStream)
        {
            Console.WriteLine(
                Encoding.UTF8.GetString(memoryStream.ToArray()));
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

    private static SnapshotStore CreateSnapshotStore(
        string repositoryPath)
    {
        return new SnapshotStore(
            CreateRepository(repositoryPath),
            new FastCdc(),
            new ConsoleProbe(),
            new RealClock(),
            new MultiPrompt(
                new EnvironmentPrompt(),
                new SecretToolPrompt(repositoryPath),
                new ConsolePrompt()));
    }

    private static IRepository CreateRepository(string repositoryPath)
    {
        return new FileRepository(repositoryPath);
    }
}
