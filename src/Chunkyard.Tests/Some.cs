namespace Chunkyard.Tests;

internal static class Some
{
    public static readonly IWorld World = new DummyWorld(DateTime.UtcNow);

    public static Crypto Crypto(string password)
    {
        return new Crypto(
            password,
            World.GenerateSalt(),
            World.Iterations);
    }

    public static Blob Blob(string blobName)
    {
        return new Blob(blobName, DateTime.UtcNow);
    }

    public static Blob[] Blobs(params string[] blobNames)
    {
        blobNames = blobNames.Any()
            ? blobNames
            : new[] { "blob 1", "dir/blob-2" };

        return blobNames
            .Select(Blob)
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
            new DummyProbe(),
            World,
            new DummyPrompt(password),
            Environment.ProcessorCount);
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
        generator ??= blobName => Encoding.UTF8.GetBytes(blobName);

        var blobSystem = new MemoryBlobSystem();

        foreach (var blob in blobs)
        {
            using var stream = blobSystem.OpenWrite(blob);

            stream.Write(generator(blob.Name));
        }

        return blobSystem;
    }
}
