namespace Chunkyard.Tests.Core;

public static class AesGcmCryptoTests
{
    [Fact]
    public static void Encrypt_And_Decrypt_Return_Input()
    {
        var expectedText = "Hello!";
        var key = AesGcmCrypto.PasswordToKey(
            "secret",
            AesGcmCrypto.GenerateSalt(),
            AesGcmCrypto.Iterations);

        var cipherText = AesGcmCrypto.Encrypt(
            AesGcmCrypto.GenerateNonce(),
            Encoding.UTF8.GetBytes(expectedText),
            key);

        var actualText = Encoding.UTF8.GetString(
            AesGcmCrypto.Decrypt(cipherText, key));

        Assert.Equal(expectedText, actualText);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public static void PasswordToKey_Throws_On_EmptyPassword(
        string password)
    {
        Assert.Throws<ArgumentException>(
            () => AesGcmCrypto.PasswordToKey(
                password,
                AesGcmCrypto.GenerateSalt(),
                AesGcmCrypto.Iterations));
    }
}
