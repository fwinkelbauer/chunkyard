using Xunit;

namespace Chunkyard.Tests
{
    public static class EncryptionProviderTests
    {
        [Fact]
        public static void GetNonce_Creates_Fixed_Random_Nonce_For_Fingerprint()
        {
            var provider = new EncryptionProvider();
            var someFingerprint = "some fingerprint";
            var differentFingerprint = "different fingerprint";

            var someNonce = provider.GetNonce(someFingerprint);
            var sameNonce = provider.GetNonce(someFingerprint);
            var differentNonce = provider.GetNonce(differentFingerprint);

            Assert.Equal(someNonce, sameNonce);
            Assert.NotEqual(someNonce, differentNonce);
        }

        [Fact]
        public static void GetNonce_Returns_Registered_Nonce_For_Fingerprint()
        {
            var provider = new EncryptionProvider();
            var fingerprint = "some fingerprint";
            var expectedNonce = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

            provider.RegisterNonce(fingerprint, expectedNonce);
            var actualNonce = provider.GetNonce(fingerprint);

            Assert.Equal(expectedNonce, actualNonce);
        }
    }
}
