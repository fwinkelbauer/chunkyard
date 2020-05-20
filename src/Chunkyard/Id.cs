using System;
using System.Security.Cryptography;
using System.Text;

namespace Chunkyard
{
    public static class Id
    {
        public static Uri ComputeContentUri(
            HashAlgorithmName hashAlgorithmName,
            byte[] data)
        {
            return ToContentUri(
                hashAlgorithmName,
                ComputeHash(hashAlgorithmName, data));
        }

        public static string ComputeHash(
            HashAlgorithmName hashAlgorithmName,
            byte[] data)
        {
            using var algorithm = HashAlgorithm.Create(hashAlgorithmName.Name);

            return ToHexString(algorithm.ComputeHash(data));
        }

        public static string ComputeHash(
            HashAlgorithmName hashAlgorithmName,
            string data)
        {
            return ComputeHash(
                hashAlgorithmName,
                Encoding.UTF8.GetBytes(data));
        }

        public static HashAlgorithmName AlgorithmFromContentUri(Uri contentUri)
        {
            contentUri.EnsureNotNull(nameof(contentUri));

            return new HashAlgorithmName(contentUri.Scheme.ToUpper());
        }

        public static string HashFromContentUri(Uri contentUri)
        {
            contentUri.EnsureNotNull(nameof(contentUri));

            // Check that this is a valid content URI
            _ = AlgorithmFromContentUri(contentUri);

            return contentUri.Host;
        }

        public static Uri ToContentUri(
            string hashAlgorithmName,
            string hash)
        {
            hashAlgorithmName.EnsureNotNull(nameof(hashAlgorithmName));

            return ToContentUri(
                new HashAlgorithmName(hashAlgorithmName.ToUpper()),
                hash);
        }

        private static Uri ToContentUri(
            HashAlgorithmName hashAlgorithmName,
            string hash)
        {
            return new Uri($"{hashAlgorithmName.Name.ToLower()}://{hash}");
        }

        private static string ToHexString(byte[] hash)
        {
            return BitConverter.ToString(hash)
                .Replace("-", string.Empty)
                .ToLower();
        }
    }
}
