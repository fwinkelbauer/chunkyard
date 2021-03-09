using System.Linq;
using System.Text;
using Chunkyard.Core;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class AesGcmCryptoTests
    {
        [Fact]
        public static void Encrypt_And_Decrypt_Return_Input()
        {
            var expectedText = "Hello!";
            var key = CreateKey();
            var nonce = AesGcmCrypto.GenerateNonce();

            var (secretText, tag) = AesGcmCrypto.Encrypt(
                Encoding.UTF8.GetBytes(expectedText),
                key,
                nonce);

            var actualText = Encoding.UTF8.GetString(
                AesGcmCrypto.Decrypt(secretText, tag, key, nonce));

            Assert.Equal(expectedText, actualText);
        }

        [Fact]
        public static void Encrypt_Returns_Nonce_Cipher_Tag()
        {
            var plainText = Encoding.UTF8.GetBytes("Hello!");
            var key = CreateKey();
            var nonce = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

            var (secretText, tag) = AesGcmCrypto.Encrypt(
                plainText,
                key,
                nonce);

            Assert.Equal(
                nonce.Length + plainText.Length + tag.Length,
                secretText.Length);

            Assert.Equal(nonce, secretText.Take(nonce.Length));
            Assert.Equal(tag, secretText.TakeLast(tag.Length));
        }

        private static byte[] CreateKey()
        {
            return AesGcmCrypto.PasswordToKey(
                "secret",
                AesGcmCrypto.GenerateSalt(),
                AesGcmCrypto.Iterations);
        }
    }
}
