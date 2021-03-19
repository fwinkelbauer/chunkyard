using System;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    /// <summary>
    /// Utility methods which are used to work with content URIs.
    ///
    /// Example: sha256://ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e
    /// </summary>
    public static class Id
    {
        public static Uri ComputeContentUri(
            HashAlgorithmName hashAlgorithmName,
            byte[] data)
        {
            return ToContentUri(
                hashAlgorithmName.Name!,
                ComputeHash(hashAlgorithmName, data));
        }

        public static string ComputeHash(
            HashAlgorithmName hashAlgorithmName,
            byte[] data)
        {
            using var algorithm = HashAlgorithm.Create(hashAlgorithmName.Name!);

            return ToHexString(algorithm!.ComputeHash(data));
        }

        public static (HashAlgorithmName HashAlgorithmName, string Hash) DeconstructContentUri(
            Uri contentUri)
        {
            contentUri.EnsureNotNull(nameof(contentUri));

            return (new HashAlgorithmName(contentUri.Scheme.ToUpper()),
                contentUri.Host);
        }

        public static Uri ToContentUri(
            string hashAlgorithmName,
            string hash)
        {
            hashAlgorithmName.EnsureNotNullOrEmpty(nameof(hashAlgorithmName));

            return new Uri($"{hashAlgorithmName.ToLower()}://{hash}");
        }

        private static string ToHexString(byte[] hash)
        {
            return BitConverter.ToString(hash)
                .Replace("-", "")
                .ToLower();
        }
    }
}
