namespace Chunkyard.Tests;

internal static class Some
{
    public static readonly DateTime UtcDate = DateTime.UtcNow;

    public static readonly Blob Blob = new Blob("some blob", UtcDate);

    public static Blob[] Blobs(params string[] blobNames)
    {
        blobNames = blobNames.Any()
            ? blobNames
            : new[] { "blob 1", "blob-2", "blob3" };

        return blobNames
            .Select(b => new Blob(b, UtcDate))
            .ToArray();
    }

    public static SnapshotStore SnapshotStore(
        IRepository? repository = null,
        FastCdc? fastCdc = null,
        string password = "secret")
    {
        return new SnapshotStore(
            repository ?? Repository(),
            fastCdc ?? new FastCdc(),
            new DummyPrompt(password),
            new DummyProbe());
    }

    public static IRepository Repository()
    {
        return new MemoryRepository();
    }

    public static IBlobSystem BlobSystem(
        IEnumerable<Blob>? blobs = null,
        Func<string, byte[]>? generate = null)
    {
        blobs ??= Array.Empty<Blob>();
        generate ??= (blobName => Encoding.UTF8.GetBytes(blobName));

        var blobSystem = new MemoryBlobSystem();

        foreach (var blob in blobs)
        {
            using var stream = blobSystem.NewWrite(blob);

            stream.Write(generate(blob.Name));
        }

        return blobSystem;
    }
}
