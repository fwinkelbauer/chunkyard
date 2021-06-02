using System;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    /// <summary>
    /// Contains methods to encrypt and decrypt data.
    /// </summary>
    public static class AesGcmCrypto
    {
        public const int Iterations = 1000;

        private const int NonceBytes = 12;
        private const int TagBytes = 16;
        private const int KeyBytes = 32;
        private const int SaltBytes = 12;

        public static byte[] Encrypt(
            byte[] nonce,
            byte[] plainText,
            byte[] key)
        {
            nonce.EnsureNotNull(nameof(nonce));
            plainText.EnsureNotNull(nameof(plainText));

            var cipherText = new byte[
                nonce.Length + plainText.Length + TagBytes];

            var innerCiphertext = new Span<byte>(
                cipherText,
                nonce.Length,
                plainText.Length);

            var tag = new Span<byte>(
                cipherText,
                cipherText.Length - TagBytes,
                TagBytes);

            using var aesGcm = new AesGcm(key);
            aesGcm.Encrypt(nonce, plainText, innerCiphertext, tag);

            Array.Copy(nonce, 0, cipherText, 0, nonce.Length);

            return cipherText;
        }

        public static byte[] Decrypt(
            byte[] cipherText,
            byte[] key)
        {
            cipherText.EnsureNotNull(nameof(cipherText));

            byte[] plainText = new byte[
                cipherText.Length - NonceBytes - TagBytes];

            var nonce = new Span<byte>(cipherText, 0, NonceBytes);

            var innerCipherText = new Span<byte>(
                cipherText,
                nonce.Length,
                plainText.Length);

            var tag = new Span<byte>(
                cipherText,
                cipherText.Length - TagBytes,
                TagBytes);

            using var aesGcm = new AesGcm(key);

            aesGcm.Decrypt(nonce, innerCipherText, tag, plainText);

            return plainText;
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
            return GenerateRandomNumber(SaltBytes);
        }

        public static byte[] GenerateNonce()
        {
            return GenerateRandomNumber(NonceBytes);
        }

        private static byte[] GenerateRandomNumber(int length)
        {
            using var randomGenerator = new RNGCryptoServiceProvider();
            var randomNumber = new byte[length];
            randomGenerator.GetBytes(randomNumber);

            return randomNumber;
        }
    }
}
