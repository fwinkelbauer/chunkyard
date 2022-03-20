namespace Chunkyard.Tests.Core;

public static class AesGcmCryptoTests
{
    [Fact]
    public static void Encrypt_And_Decrypt_Return_Input()
    {
        var aesGcmCrypto = new AesGcmCrypto(
            "secret",
            AesGcmCrypto.GenerateSalt(),
            AesGcmCrypto.DefaultIterations);

        var plainBytes = Encoding.UTF8.GetBytes("Hello!");

        var encryptedBytes = aesGcmCrypto.Encrypt(
            AesGcmCrypto.GenerateNonce(),
            plainBytes);

        var decryptedBytes = aesGcmCrypto.Decrypt(encryptedBytes);

        Assert.NotEqual(plainBytes, encryptedBytes);
        Assert.Equal(plainBytes, decryptedBytes);
    }

    [Fact]
    public static void Decrypt_Throws_Given_Wrong_Key()
    {
        var someAes = new AesGcmCrypto(
            "some secret",
            AesGcmCrypto.GenerateSalt(),
            AesGcmCrypto.DefaultIterations);

        var otherAes = new AesGcmCrypto(
            "other secret",
            AesGcmCrypto.GenerateSalt(),
            AesGcmCrypto.DefaultIterations);

        var plainBytes = Encoding.UTF8.GetBytes("Hello!");

        var encryptedBytes = someAes.Encrypt(
            AesGcmCrypto.GenerateNonce(),
            Encoding.UTF8.GetBytes("Hello!"));

        Assert.Throws<CryptographicException>(
            () => otherAes.Decrypt(encryptedBytes));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public static void Constructor_Throws_On_EmptyPassword(
        string password)
    {
        Assert.Throws<ArgumentException>(
            () => new AesGcmCrypto(
                password,
                AesGcmCrypto.GenerateSalt(),
                AesGcmCrypto.DefaultIterations));
    }
}
