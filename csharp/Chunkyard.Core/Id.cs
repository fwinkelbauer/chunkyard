using System;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    public static class Id
    {
        private const string LogScheme = "log";
        private const string QueryId = "id";

        public static Uri LogNameToUri(string logName)
        {
            return new Uri($"{LogScheme}://{logName}");
        }

        public static Uri LogNameToUri(string logName, int logPosition)
        {
            return new Uri($"{LogScheme}://{logName}?id={logPosition}");
        }

        public static (string, int?) LogUriToParts(Uri logUri)
        {
            if (!logUri.Scheme.Equals(LogScheme))
            {
                throw new ChunkyardException($"Not a reference log URI: {logUri}");
            }

            var queryValues = System.Web.HttpUtility.ParseQueryString(logUri.Query);
            var logText = queryValues.Get(QueryId);
            int? logPosition = null;

            if (int.TryParse(logText, out var number))
            {
                logPosition = number;
            }

            return (logUri.Host, logPosition);
        }

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

        public static string HashFromContentUri(Uri contentUri)
        {
            // Verify that this is a content URI
            _ = AlgorithmFromContentUri(contentUri);

            return contentUri.Host;
        }

        public static Uri ToContentUri(HashAlgorithmName algorithmName, string hash)
        {
            return new Uri($"{algorithmName.Name.ToLower()}://{hash}");
        }

        private static string ToHexString(byte[] hash)
        {
            return BitConverter.ToString(hash)
                .Replace("-", string.Empty)
                .ToLower();
        }
    }
}
