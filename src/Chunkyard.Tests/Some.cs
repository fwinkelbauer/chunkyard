namespace Chunkyard.Tests;

internal static class Some
{
    public static readonly IClock Clock = new DummyClock();

    public static Crypto Crypto(string password = "secret")
    {
        return new DummyCryptoFactory(password).Create(null);
    }

    public static Blob Blob(string blobName)
    {
        return new Blob(blobName, Clock.UtcNow());
    }

    public static Blob[] Blobs(params string[] blobNames)
    {
        blobNames = blobNames.Length > 0
            ? blobNames
            : new[] { "blob 1", "dir/blob-2" };

        return blobNames
            .Select(Blob)
            .ToArray();
    }

    public static SnapshotStore SnapshotStore(
        IRepository? repository = null,
        IChunker? chunker = null,
        ICryptoFactory? cryptoFactory = null)
    {
        return new SnapshotStore(
            repository ?? Repository(),
            chunker ?? new DummyChunker(),
            new DummyProbe(),
            Clock,
            cryptoFactory ?? new DummyCryptoFactory("secret"));
    }

    public static IRepository Repository()
    {
        return new MemoryRepository();
    }

    public static IBlobSystem BlobSystem(
        IEnumerable<Blob>? blobs = null,
        Func<string, byte[]>? generator = null)
    {
        blobs ??= Array.Empty<Blob>();
        generator ??= Encoding.UTF8.GetBytes;

        var blobSystem = new MemoryBlobSystem();

        foreach (var blob in blobs)
        {
            using var stream = blobSystem.OpenWrite(blob);

            stream.Write(generator(blob.Name));
        }

        return blobSystem;
    }

    public static string Directory()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "chunkyard-test",
            Path.GetRandomFileName());
    }

    public static Dictionary<string, IReadOnlyCollection<string>> Flags(
        params (string Key, IReadOnlyCollection<string> Value)[] pairs)
    {
        return pairs.ToDictionary(p => p.Key, p => p.Value);
    }

    public static FlagConsumer FlagConsumer(
        params (string Key, IReadOnlyCollection<string> Value)[] pairs)
    {
        return new FlagConsumer(
            Flags(pairs),
            new HelpCommandBuilder("Chunkyard.Tests"));
    }

    public static string[] Strings(params string[] values)
    {
        return values;
    }
}
