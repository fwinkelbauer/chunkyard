﻿using System;
using System.Security.Cryptography;
using System.Text;

namespace Chunkyard.Core
{
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
                ComputeHash(AlgorithmSha256, content));
        }

        public static bool ContentUriValid(
            Uri contentUri,
            ReadOnlySpan<byte> content)
        {
            var (hashAlgorithmName, hash) = DeconstructContentUri(contentUri);
            var computedHash = ComputeHash(hashAlgorithmName, content);

            return hash.Equals(computedHash);
        }

        public static (string HashAlgorithmName, string Hash) DeconstructContentUri(
            Uri contentUri)
        {
            ArgumentNullException.ThrowIfNull(contentUri);

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
            string hashAlgorithmName,
            ReadOnlySpan<byte> content)
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

            return ToHexString(SHA256.HashData(content));
        }

        private static string ToHexString(byte[] hash)
        {
            var builder = new StringBuilder();

            foreach (var h in hash)
            {
                builder.Append(h.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
