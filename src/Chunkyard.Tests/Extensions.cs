namespace Chunkyard.Tests;

/// <summary>
/// A collection of extension methods.
/// </summary>
internal static class Extensions
{
    public static int StoreSnapshot(
        this SnapshotStore snapshotStore,
        IBlobSystem blobSystem)
    {
        return snapshotStore.StoreSnapshot(
            blobSystem,
            Some.UtcNow(),
            new Fuzzy());
    }

    public static bool CheckSnapshot(
        this SnapshotStore snapshotStore,
        int snapshotId)
    {
        return snapshotStore.CheckSnapshot(snapshotId, new Fuzzy());
    }

    public static void RestoreSnapshot(
        this SnapshotStore snapshotStore,
        IBlobSystem blobSystem,
        int snapshotId)
    {
        snapshotStore.RestoreSnapshot(blobSystem, snapshotId, new Fuzzy());
    }

    public static void Missing<T>(
        this IRepository<T> repository,
        IEnumerable<T> keys)
    {
        foreach (var key in keys)
        {
            repository.Remove(key);
        }
    }

    public static void Corrupt<T>(
        this IRepository<T> repository,
        IEnumerable<T> keys)
    {
        foreach (var key in keys)
        {
            var bytes = repository.Retrieve(key);

            repository.Remove(key);
            repository.Write(
                key,
                bytes.Concat(new byte[] { 0xFF }).ToArray());
        }
    }

    public static Dictionary<Blob, string> ToDictionary(
        this IBlobSystem blobSystem)
    {
        return blobSystem.ListBlobs().ToDictionary(
            blob => blob,
            blob =>
            {
                using var memoryStream = new MemoryStream();
                using var blobStream = blobSystem.OpenRead(blob.Name);

                blobStream.CopyTo(memoryStream);

                return Convert.ToHexString(memoryStream.ToArray());
            });
    }
}
