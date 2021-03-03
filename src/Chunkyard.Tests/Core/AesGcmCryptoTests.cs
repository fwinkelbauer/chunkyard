﻿using System.Text;
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

            var password = "secret";
            var salt = AesGcmCrypto.GenerateSalt();
            var iterations = AesGcmCrypto.Iterations;
            var nonce = AesGcmCrypto.GenerateNonce();

            var key = AesGcmCrypto.PasswordToKey(password, salt, iterations);

            var (secretText, tag) = AesGcmCrypto.Encrypt(
                Encoding.UTF8.GetBytes(expectedText),
                key,
                nonce);

            var actualText = Encoding.UTF8.GetString(
                AesGcmCrypto.Decrypt(secretText, tag, key, nonce));

            Assert.Equal(expectedText, actualText);
        }
    }
}