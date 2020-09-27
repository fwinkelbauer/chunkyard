using System.Security.Cryptography;

namespace Chunkyard
{
    /// <summary>
    /// Contains methods to encrypt and decrypt data.
    /// </summary>
    public static class AesGcmCrypto
    {
        public const int Iterations = 1000;

        private const int TagBytes = 16;
        private const int KeyBytes = 32;
        private const int NonceBytes = 12;
        private const int SaltBytes = 12;

        public static (byte[] Ciphertext, byte[] Tag) Encrypt(
            byte[] plaintext,
            byte[] key,
            byte[] nonce)
        {
            plaintext.EnsureNotNull(nameof(plaintext));

            var tag = new byte[TagBytes];
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
