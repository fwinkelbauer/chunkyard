namespace Chunkyard.Core;

/// <summary>
/// Utility methods which are used to work with chunk IDs.
///
/// Example: ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e
/// </summary>
public static class ChunkId
{
    public static string Compute(ReadOnlySpan<byte> chunk)
    {
        return Convert.ToHexString(SHA256.HashData(chunk)).ToLower();
    }

    public static bool Valid(string chunkId, ReadOnlySpan<byte> chunk)
    {
        return Compute(chunk).Equals(chunkId);
    }

    public static string Shorten(string chunkId)
    {
        ArgumentNullException.ThrowIfNull(chunkId);

        return chunkId.Substring(0, 12);
    }
}
