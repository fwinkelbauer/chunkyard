namespace Chunkyard.Core;

/// <summary>
/// Utility methods which are used to work with chunk IDs.
///
/// Example: sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e
/// </summary>
public static class ChunkId
{
    public const string HashAlgorithmName = "sha256";

    public static Uri Compute(ReadOnlySpan<byte> chunk)
    {
        return Construct(
            HashAlgorithmName,
            Convert.ToHexString(SHA256.HashData(chunk)).ToLower());
    }

    public static bool Valid(Uri chunkId, ReadOnlySpan<byte> chunk)
    {
        return Compute(chunk).Equals(chunkId);
    }

    public static (string HashAlgorithmName, string Hash) Deconstruct(
        Uri chunkId)
    {
        ArgumentNullException.ThrowIfNull(chunkId);

        if (!chunkId.Scheme.Equals(HashAlgorithmName))
        {
            throw new NotSupportedException();
        }

        return (chunkId.Scheme, chunkId.Host);
    }

    public static Uri Construct(
        string hashAlgorithmName,
        string hash)
    {
        ArgumentNullException.ThrowIfNull(hashAlgorithmName);

        return new Uri($"{hashAlgorithmName}://{hash}");
    }
}
