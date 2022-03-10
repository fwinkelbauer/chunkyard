namespace Chunkyard;

/// <summary>
/// Describes every available command line verb of the Chunkyard assembly.
/// </summary>
internal static class Commands
{
    public static void PreviewSnapshot(PreviewOptions o)
    {
        var blobSystem = new FileBlobSystem(o.Files);
        var blobs = blobSystem.ListBlobs(
            new Fuzzy(o.ExcludePatterns));

        var snapshotStore = CreateSnapshotStore(o.Repository);

        var blobReferences = snapshotStore.CurrentSnapshot?.BlobReferences
            ?? Array.Empty<BlobReference>();

        var diff = DiffSet.Create(
            blobReferences.Select(blobReference => blobReference.Blob),
            blobs,
            blob => blob.Name);

        PrintDiff(diff);
    }

    public static void CreateSnapshot(CreateOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.Repository);

        snapshotStore.StoreSnapshot(
            new FileBlobSystem(o.Files),
            new Fuzzy(o.ExcludePatterns),
            DateTime.UtcNow);
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
                Console.WriteLine(chunkId.AbsoluteUri);
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
            new FileBlobSystem(new[] { o.Directory }),
            o.SnapshotId,
            new Fuzzy(o.IncludePatterns));
    }

    public static void MirrorSnapshot(MirrorOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.Repository);

        snapshotStore.MirrorSnapshot(
            new FileBlobSystem(o.Files),
            new Fuzzy(o.ExcludePatterns),
            o.SnapshotId);
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
                chunkId => chunkId.AbsoluteUri)
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
        var uriRepository = CreateUriRepository(o.DestinationRepository);
        var intRepository = CreateIntRepository(o.DestinationRepository);

        snapshotStore.Copy(uriRepository, intRepository);
    }

    public static void Cat(CatOptions o)
    {
        var snapshotStore = CreateSnapshotStore(o.Repository);

        if (string.IsNullOrEmpty(o.Export))
        {
            using var stream = new MemoryStream();
            snapshotStore.RestoreChunks(o.ChunkIds, stream);

            Console.WriteLine(
                DataConvert.BytesToText(stream.ToArray()));
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
            CreateUriRepository(repositoryPath),
            CreateIntRepository(repositoryPath),
            new FastCdc(),
            new EnvironmentPrompt(
                new ConsolePrompt()),
            new ConsoleProbe());
    }

    private static IRepository<Uri> CreateUriRepository(
        string repositoryPath)
    {
        return FileRepository.CreateUriRepository(
            Path.Combine(repositoryPath, "chunks"));
    }

    private static IRepository<int> CreateIntRepository(
        string repositoryPath)
    {
        return FileRepository.CreateIntRepository(
            Path.Combine(repositoryPath, "snapshots"));
    }
}
