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
}
