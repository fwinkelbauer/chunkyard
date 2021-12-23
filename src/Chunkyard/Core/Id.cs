namespace Chunkyard.Core;

/// <summary>
/// Utility methods which are used to work with content URIs.
///
/// Example: sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e
/// </summary>
public static class Id
{
    private const string AlgorithmSha256 = "sha256";

    public static Uri ComputeContentUri(
        ReadOnlySpan<byte> content)
    {
        return ToContentUri(
            AlgorithmSha256,
            ComputeHash(content));
    }

    public static bool ContentUriValid(
        Uri contentUri,
        ReadOnlySpan<byte> content)
    {
        var (_, hash) = DeconstructContentUri(contentUri);

        return hash.Equals(ComputeHash(content));
    }

    public static (string HashAlgorithmName, string Hash) DeconstructContentUri(
        Uri contentUri)
    {
        ArgumentNullException.ThrowIfNull(contentUri);

        if (!contentUri.Scheme.Equals(AlgorithmSha256))
        {
            throw new NotSupportedException();
        }

        return (contentUri.Scheme, contentUri.Host);
    }

    public static Uri ToContentUri(
        string hashAlgorithmName,
        string hash)
    {
        ArgumentNullException.ThrowIfNull(hashAlgorithmName);

        return new Uri($"{hashAlgorithmName}://{hash}");
    }

    private static string ComputeHash(
        ReadOnlySpan<byte> content)
    {
        return Convert.ToHexString(SHA256.HashData(content))
            .ToLower();
    }
}
