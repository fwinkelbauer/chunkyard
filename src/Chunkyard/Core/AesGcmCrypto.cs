﻿using System;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    /// <summary>
    /// Contains methods to encrypt and decrypt data.
    /// </summary>
    public static class AesGcmCrypto
    {
        public const int Iterations = 1000;
        public const int NonceBytes = 12;
        public const int TagBytes = 16;

        private const int KeyBytes = 32;
        private const int SaltBytes = 12;

        public static (byte[] Ciphertext, byte[] Tag) Encrypt(
            byte[] plaintext,
            byte[] key,
            byte[] nonce)
        {
            plaintext.EnsureNotNull(nameof(plaintext));
            nonce.EnsureNotNull(nameof(nonce));

            var tag = new byte[TagBytes];

            var buffer = new byte[nonce.Length + plaintext.Length + tag.Length];
            var ciphertext = new Span<byte>(buffer, nonce.Length, plaintext.Length);

            using var aesGcm = new AesGcm(key);
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

            // We add all cryptographic details needed to decrypt a piece of
            // content so that we can recover it even if we lose our meta data
            Array.Copy(nonce, 0, buffer, 0, nonce.Length);
            Array.Copy(tag, 0, buffer, buffer.Length - tag.Length, tag.Length);

            return (buffer, tag);
        }

        public static byte[] Decrypt(
            byte[] ciphertext,
            byte[] tag,
            byte[] key,
            byte[] nonce)
        {
            ciphertext.EnsureNotNull(nameof(ciphertext));
            tag.EnsureNotNull(nameof(tag));
            nonce.EnsureNotNull(nameof(nonce));

            byte[] plaintext = new byte[ciphertext.Length - nonce.Length - tag.Length];

            // Strip away the cryptographic details which we added when
            // encrypting the value
            var buffer = new Span<byte>(ciphertext, nonce.Length, plaintext.Length);

            using var aesGcm = new AesGcm(key);

            try
            {
                aesGcm.Decrypt(nonce, buffer, tag, plaintext);
            }
            catch (Exception e)
            {
                throw new ChunkyardException(
                    "Could not decrypt data",
                    e);
            }

            return plaintext;
        }

        public static byte[] PasswordToKey(
            string password,
            byte[] salt,
            int iterations)
        {
            password.EnsureNotNullOrEmpty(nameof(password));

            using var rfc2898 = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256);

            return rfc2898.GetBytes(KeyBytes);
        }

        public static byte[] GenerateSalt()
        {
            return GenerateRandomMumber(SaltBytes);
        }

        public static byte[] GenerateNonce()
        {
            return GenerateRandomMumber(NonceBytes);
        }

        private static byte[] GenerateRandomMumber(int length)
        {
            using var randomGenerator = new RNGCryptoServiceProvider();
            var randomNumber = new byte[length];
            randomGenerator.GetBytes(randomNumber);

            return randomNumber;
        }
    }
}
