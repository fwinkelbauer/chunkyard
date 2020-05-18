using Xunit;

namespace Chunkyard.Tests
{
    public static class EncryptionProviderTests
    {
        [Fact]
        public static void GetNonce_Creates_Fixed_Random_Nonce_For_Files()
        {
            var provider = new EncryptionProvider();
            var someFile = "some file";
            var differentFile = "different file";

            var someNonce = provider.GetNonce(someFile);
            var sameNonce = provider.GetNonce(someFile);
            var differentNonce = provider.GetNonce(differentFile);

            Assert.Equal(someNonce, sameNonce);
            Assert.NotEqual(someNonce, differentNonce);
        }

        [Fact]
        public static void GetNonce_Returns_Registered_Nonce_For_Files()
        {
            var provider = new EncryptionProvider();
            var file = "some file";
            var expectedNonce = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

            provider.RegisterNonce(file, expectedNonce);
            var actualNonce = provider.GetNonce(file);

            Assert.Equal(expectedNonce, actualNonce);
        }
    }
}
