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
        public const string AlgorithmSha256 = "SHA256";

        public static Uri ComputeContentUri(
            string hashAlgorithmName,
            byte[] content)
        {
            hashAlgorithmName.EnsureNotNullOrEmpty(nameof(hashAlgorithmName));

            return ToContentUri(
                hashAlgorithmName,
                ComputeHash(hashAlgorithmName, content));
        }

        public static bool ContentUriValid(
            Uri contentUri,
            byte[] content)
        {
            var (hashAlgorithmName, hash) = DeconstructContentUri(contentUri);
            var computedHash = ComputeHash(hashAlgorithmName, content);

            return hash.Equals(computedHash);
        }

        public static (string HashAlgorithmName, string Hash) DeconstructContentUri(
            Uri contentUri)
        {
            contentUri.EnsureNotNull(nameof(contentUri));

            return (contentUri.Scheme.ToUpper(), contentUri.Host);
        }

        public static Uri ToContentUri(
            string hashAlgorithmName,
            string hash)
        {
            hashAlgorithmName.EnsureNotNullOrEmpty(nameof(hashAlgorithmName));

            return new Uri($"{hashAlgorithmName.ToLower()}://{hash}");
        }

        private static string ComputeHash(
            string hashAlgorithmName,
            byte[] content)
        {
            // We are publishing Chunkyard using the "-p:TrimMode=Link" compiler
            // option. This option cuts the binary size in half, but throws null
            // pointer Exceptions when running Chunkyard if we create the
            // algorithm dynamically using
            // "HashAlgorithm.Create(hashAlgorithmName)"
            if (!hashAlgorithmName.Equals(AlgorithmSha256))
            {
                throw new NotSupportedException();
            }

            using var algorithm = SHA256.Create();

            return ToHexString(algorithm.ComputeHash(content));
        }

        private static string ToHexString(byte[] hash)
        {
            return BitConverter.ToString(hash)
                .Replace("-", "")
                .ToLower();
        }
    }
}
