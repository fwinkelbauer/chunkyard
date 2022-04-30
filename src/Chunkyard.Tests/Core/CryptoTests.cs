namespace Chunkyard.Tests.Core;

public static class CryptoTests
{
    [Fact]
    public static void Encrypt_And_Decrypt_Return_Input()
    {
        var crypto = new Crypto(
            "secret",
            Crypto.GenerateSalt(),
            Crypto.DefaultIterations);

        var plainBytes = Encoding.UTF8.GetBytes("Hello!");

        var encryptedBytes = crypto.Encrypt(
            Crypto.GenerateNonce(),
            plainBytes);

        var decryptedBytes = crypto.Decrypt(encryptedBytes);

        Assert.NotEqual(plainBytes, encryptedBytes);
        Assert.Equal(plainBytes, decryptedBytes);
    }

    [Fact]
    public static void Decrypt_Throws_Given_Wrong_Key()
    {
        var someCrypto = new Crypto(
            "some secret",
            Crypto.GenerateSalt(),
            Crypto.DefaultIterations);

        var otherCrypto = new Crypto(
            "other secret",
            Crypto.GenerateSalt(),
            Crypto.DefaultIterations);

        var plainBytes = Encoding.UTF8.GetBytes("Hello!");

        var encryptedBytes = someCrypto.Encrypt(
            Crypto.GenerateNonce(),
            Encoding.UTF8.GetBytes("Hello!"));

        Assert.Throws<CryptographicException>(
            () => otherCrypto.Decrypt(encryptedBytes));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public static void Constructor_Throws_On_EmptyPassword(
        string password)
    {
        Assert.Throws<ArgumentException>(
            () => new Crypto(
                password,
                Crypto.GenerateSalt(),
                Crypto.DefaultIterations));
    }
}
