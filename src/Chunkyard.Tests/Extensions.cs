namespace Chunkyard.Tests;

/// <summary>
/// A collection of extension methods.
/// </summary>
internal static class Extensions
{
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
