namespace Chunkyard.Tests;

/// <summary>
/// A collection of extension methods.
/// </summary>
internal static class Extensions
{
    extension(SnapshotStore snapshotStore)
    {
        public int StoreSnapshot(
            IBlobSystem blobSystem)
        {
            return snapshotStore.StoreSnapshot(
                blobSystem,
                Some.UtcNow(),
                new Fuzzy());
        }

        public bool CheckSnapshot(
            int snapshotId)
        {
            return snapshotStore.CheckSnapshot(snapshotId, new Fuzzy());
        }

        public void RestoreSnapshot(
            IBlobSystem blobSystem,
            int snapshotId)
        {
            snapshotStore.RestoreSnapshot(blobSystem, snapshotId, new Fuzzy());
        }
    }

    extension<T>(IRepository<T> repository)
    {
        public void Missing(IEnumerable<T> keys)
        {
            foreach (var key in keys)
            {
                repository.Remove(key);
            }
        }

        public void Corrupt(IEnumerable<T> keys)
        {
            foreach (var key in keys)
            {
                repository.Remove(key);
                repository.Write(key, new byte[] { 0xFF });
            }
        }
    }

    extension(IBlobSystem blobSystem)
    {
        public Dictionary<Blob, string> ToDictionary()
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
}
