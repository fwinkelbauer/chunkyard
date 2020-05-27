using System.Security.Cryptography;

namespace Chunkyard
{
    /// <summary>
    /// Contains methods to encrypt and decrypt data.
    /// </summary>
    public static class AesGcmCrypto
    {
        public const int Iterations = 1000;

        private const int TAG_BYTES = 16;
        private const int KEY_BYTES = 32;
        private const int NONCE_BYTES = 12;
        private const int SALT_BYTES = 12;

        public static (byte[], byte[]) Encrypt(
            byte[] plaintext,
            byte[] key,
            byte[] nonce)
        {
            plaintext.EnsureNotNull(nameof(plaintext));

            var tag = new byte[TAG_BYTES];
            var ciphertext = new byte[plaintext.Length];

            using var aesGcm = new AesGcm(key);
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

            return (ciphertext, tag);
        }

        public static byte[] Decrypt(
            byte[] ciphertext,
            byte[] tag,
            byte[] key,
            byte[] nonce)
        {
            ciphertext.EnsureNotNull(nameof(ciphertext));

            byte[] plaintext = new byte[ciphertext.Length];

            using var aesGcm = new AesGcm(key);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

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

            return rfc2898.GetBytes(KEY_BYTES);
        }

        public static byte[] GenerateSalt()
        {
            return GenerateRandomMumber(SALT_BYTES);
        }

        public static byte[] GenerateNonce()
        {
            return GenerateRandomMumber(NONCE_BYTES);
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
