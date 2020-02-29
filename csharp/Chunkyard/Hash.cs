using System;
using System.Security.Cryptography;

namespace Chunkyard
{
    public static class Hash
    {
        public static Uri ComputeContentUri(HashAlgorithmName algorithmName, byte[] data)
        {
            using var algorithm = HashAlgorithm.Create(algorithmName.Name);

            return ToContentUri(
                algorithmName,
                ToHexString(algorithm.ComputeHash(data)));
        }

        public static HashAlgorithmName AlgorithmFromContentUri(Uri contentUri)
        {
            return new HashAlgorithmName(contentUri.Scheme);
        }

        public static string HashFromContentUri(Uri uri)
        {
            return uri.Host;
        }

        public static Uri ToContentUri(string algorithmName, string hash)
        {
            return new Uri($"{algorithmName.ToLower()}://{hash}");
        }

        private static Uri ToContentUri(HashAlgorithmName algorithmName, string hash)
        {
            return ToContentUri(algorithmName.Name, hash);
        }

        private static string ToHexString(byte[] hash)
        {
            return BitConverter.ToString(hash)
                .Replace("-", string.Empty)
                .ToLower();
        }
    }
}
