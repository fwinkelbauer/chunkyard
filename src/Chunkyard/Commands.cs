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
            new Fuzzy(o.ExcludePatterns));

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

        if (o.ChunksOnly)
        {
            var chunkIds = blobReferences.SelectMany(b => b.ChunkIds);

            foreach (var chunkId in chunkIds)
            {
                Console.WriteLine(ChunkId.Shorten(chunkId));
            }
        }
        else
        {
            foreach (var blobReference in blobReferences)
            {
                Console.WriteLine(blobReference.Blob.Name);
            }
        }
    }

    public static void RestoreSnapshot(RestoreOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.Repository);

        snapshotStore.RestoreSnapshot(
            new FileBlobSystem(new[] { o.Directory }, Fuzzy.Default),
            o.SnapshotId,
            new Fuzzy(o.IncludePatterns));
    }

    public static void MirrorSnapshot(MirrorOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.Repository);

        var blobReferences = snapshotStore.FilterSnapshot(
            o.SnapshotId,
            new Fuzzy(o.IncludePatterns));

        var paths = blobReferences
            .Select(br => DirectoryUtils.GetRootParent(br.Blob.Name))
            .Distinct()
            .Select(file => Path.Combine(o.Directory, file));

        var blobSystem = new FileBlobSystem(
            paths,
            new Fuzzy(o.ExcludePatterns));

        if (o.Preview)
        {
            var diffSet = snapshotStore.MirrorSnapshotPreview(
                blobSystem,
                o.SnapshotId);

            PrintDiff(diffSet);
        }
        else
        {
            snapshotStore.MirrorSnapshot(
                blobSystem,
                o.SnapshotId);
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
                first.SelectMany(b => b.ChunkIds),
                second.SelectMany(b => b.ChunkIds),
                chunkId => ChunkId.Shorten(chunkId))
            : DiffSet.Create(
                first,
                second,
                blobReference => blobReference.Blob.Name);

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

        if (string.IsNullOrEmpty(o.Export))
        {
            using var stream = new MemoryStream();
            snapshotStore.RestoreChunks(o.ChunkIds, stream);

            Console.WriteLine(
                Encoding.UTF8.GetString(stream.ToArray()));
        }
        else
        {
            using var stream = new FileStream(
                o.Export,
                FileMode.CreateNew,
                FileAccess.Write);

            snapshotStore.RestoreChunks(o.ChunkIds, stream);
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
            new ChunkStore(
                CreateRepository(repositoryPath),
                new FastCdc(),
                new MultiPrompt(
                    new EnvironmentPrompt(),
                    new SecretToolPrompt(repositoryPath),
                    new ConsolePrompt())),
            new ConsoleProbe(),
            new RealClock());
    }

    private static IRepository CreateRepository(string repositoryPath)
    {
        return new FileRepository(repositoryPath);
    }
}
