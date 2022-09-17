namespace Chunkyard.Tests;

internal static class Some
{
    public static readonly DateTime DateUtc = DateTime.UtcNow;

    public static Blob Blob(string blobName)
    {
        return new Blob(blobName, DateUtc);
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
        Repository? repository = null,
        FastCdc? fastCdc = null,
        string password = "secret")
    {
        return new SnapshotStore(
            repository ?? Repository(),
            fastCdc ?? new FastCdc(),
            new DummyProbe(),
            new DummyClock(DateUtc),
            new DummyPrompt(password));
    }

    public static Repository Repository()
    {
        return MemoryRepository.Create();
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

    public static byte[] RandomNumber(int length)
    {
        return RandomNumberGenerator.GetBytes(length);
    }
}
