﻿using System;
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
            byte[] content)
        {
            return ToContentUri(
                hashAlgorithmName.Name!,
                ComputeHash(hashAlgorithmName, content));
        }

        public static bool ContentUriValid(
            Uri contentUri,
            byte[] content)
        {
            var (algorithm, hash) = DeconstructContentUri(contentUri);
            var computedHash = ComputeHash(algorithm, content);

            return hash.Equals(computedHash);
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

        private static string ComputeHash(
            HashAlgorithmName hashAlgorithmName,
            byte[] content)
        {
            using var algorithm = HashAlgorithm.Create(hashAlgorithmName.Name!);

            return ToHexString(algorithm!.ComputeHash(content));
        }

        private static string ToHexString(byte[] hash)
        {
            return BitConverter.ToString(hash)
                .Replace("-", "")
                .ToLower();
        }
    }
}
