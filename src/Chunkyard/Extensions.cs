namespace Chunkyard;

/// <summary>
/// A collection of extension methods.
/// </summary>
public static class Extensions
{
    public static IEnumerable<Blob> ListBlobs(
        this IBlobSystem blobSystem,
        Fuzzy fuzzy)
    {
        return blobSystem.ListBlobs()
            .Where(b => fuzzy.IsMatch(b.Name));
    }

    public static Blob[] ListBlobs(
        this Snapshot snapshot,
        Fuzzy fuzzy)
    {
        return snapshot.BlobReferences
            .Select(br => br.Blob)
            .Where(b => fuzzy.IsMatch(b.Name))
            .ToArray();
    }

    public static BlobReference[] ListBlobReferences(
        this Snapshot snapshot,
        Fuzzy fuzzy)
    {
        return snapshot.BlobReferences
            .Where(b => fuzzy.IsMatch(b.Blob.Name))
            .ToArray();
    }

    public static bool TrySnapshotStore(
        this FlagConsumer consumer,
        out SnapshotStore snapshotStore)
    {
        var success = consumer.TryRepository("--repository", "The repository path", out var repository)
            & consumer.TryEnum("--password", "The password prompt method", out Password password, Password.Console);

        ICryptoFactory cryptoFactory = password switch
        {
            Password.Console => new ConsoleCryptoFactory(),
            Password.Libsecret => new LibsecretCryptoFactory(new ConsoleCryptoFactory()),
            _ => new ConsoleCryptoFactory()
        };

        _ = consumer.TryDryRun(cryptoFactory, c => new DryRunCryptoFactory(c), out cryptoFactory);

        snapshotStore = new SnapshotStore(
            repository,
            new SimpleChunker(),
            new ConsoleProbe(),
            cryptoFactory);

        return success;
    }

    public static bool TryRepository(
        this FlagConsumer consumer,
        string flag,
        string info,
        out IRepository repository)
    {
        var success = consumer.TryString(flag, info, out var path)
            & consumer.TryDryRun(new FileRepository(path), r => new DryRunRepository(r), out repository);

        return success;
    }

    public static bool TryBlobSystem(
        this FlagConsumer consumer,
        string flag,
        string info,
        out IBlobSystem blobSystem)
    {
        var success = consumer.TryStrings(flag, info, out var directories)
            & consumer.TryDryRun(new FileBlobSystem(directories), b => new DryRunBlobSystem(b), out blobSystem);

        return success;
    }

    public static bool TrySnapshot(
        this FlagConsumer consumer,
        out int snapshot)
    {
        return consumer.TryInt(
            "--snapshot",
            "The snapshot ID",
            out snapshot,
            SnapshotStore.LatestSnapshotId);
    }

    public static bool TryInclude(
        this FlagConsumer consumer,
        out Fuzzy include)
    {
        var success = consumer.TryStrings(
            "--include",
            "A list of fuzzy patterns for files to include",
            out var includePatterns,
            Array.Empty<string>());

        include = success
            ? new Fuzzy(includePatterns)
            : new Fuzzy();

        return success;
    }

    public static bool TryDryRun<T>(
        this FlagConsumer consumer,
        T input,
        Func<T, T> decorator,
        out T output)
    {
        var success = consumer.TryBool(
            "--dry-run",
            "Do not persist any data changes",
            out var dryRun);

        output = dryRun ? decorator(input) : input;

        return success;
    }
}

public enum Password
{
    Console = 0,
    Libsecret = 1
}
