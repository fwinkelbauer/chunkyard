namespace Chunkyard.Tests;

/// <summary>
/// A collection of object factories that are useful when writing tests.
/// </summary>
internal static class Some
{
    private static DateTime Clock = DateTime.UtcNow;

    public static DateTime UtcNow()
    {
        Clock = Clock.AddSeconds(1);

        return Clock;
    }

    public static Crypto Crypto(string password = "secret")
    {
        return new DummyCryptoFactory(password).Create(null);
    }

    public static Blob Blob(string blobName)
    {
        return new Blob(blobName, UtcNow());
    }

    public static Blob[] Blobs(params string[] blobNames)
    {
        blobNames = blobNames.Length > 0
            ? blobNames
            : new[]
            {
                Path.GetRandomFileName(),
                $"dir/{Path.GetRandomFileName()}"
            };

        return blobNames
            .Select(Blob)
            .ToArray();
    }

    public static SnapshotStore SnapshotStore(
        IRepository? repository = null,
        ICryptoFactory? cryptoFactory = null)
    {
        return new SnapshotStore(
            repository ?? Repository(),
            new DummyProbe(),
            cryptoFactory ?? new DummyCryptoFactory("secret"));
    }

    public static IRepository Repository()
    {
        return new MemoryRepository();
    }

    public static IBlobSystem BlobSystem(
        IEnumerable<Blob>? blobs = null,
        byte[]? content = null)
    {
        blobs ??= Array.Empty<Blob>();

        var blobSystem = new MemoryBlobSystem();

        foreach (var blob in blobs)
        {
            using var stream = blobSystem.OpenWrite(blob);

            content ??= RandomNumberGenerator.GetBytes(
                RandomNumberGenerator.GetInt32(16, 32));

            stream.Write(content);
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
