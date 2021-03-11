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

            var cipherText = AesGcmCrypto.Encrypt(
                AesGcmCrypto.GenerateNonce(),
                Encoding.UTF8.GetBytes(expectedText),
                key);

            var actualText = Encoding.UTF8.GetString(
                AesGcmCrypto.Decrypt(cipherText, key));

            Assert.Equal(expectedText, actualText);
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
