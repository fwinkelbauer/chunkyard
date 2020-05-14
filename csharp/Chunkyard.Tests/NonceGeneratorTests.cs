using System;
using Xunit;

namespace Chunkyard.Tests
{
    public static class NonceGeneratorTests
    {
        [Fact]
        public static void GetNonce_Creates_Fixed_Random_Nonce_For_Fingerprint()
        {
            var generator = new NonceGenerator();
            var someFingerprint = "some fingerprint";
            var differentFingerprint = "different fingerprint";

            var someNonce = generator.GetNonce(someFingerprint);
            var sameNonce = generator.GetNonce(someFingerprint);
            var differentNonce = generator.GetNonce(differentFingerprint);

            Assert.Equal(someNonce, sameNonce);
            Assert.NotEqual(someNonce, differentNonce);
        }

        [Fact]
        public static void GetNonce_Returns_Registered_Nonce_For_Fingerprint()
        {
            var generator = new NonceGenerator();
            var fingerprint = "some fingerprint";
            var expectedNonce = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF};

            generator.Register(fingerprint, expectedNonce);
            var actualNonce = generator.GetNonce(fingerprint);

            Assert.Equal(expectedNonce, actualNonce);
        }
    }
}
