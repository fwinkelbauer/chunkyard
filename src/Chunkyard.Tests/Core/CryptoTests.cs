namespace Chunkyard.Tests.Core;

public static class CryptoTests
{
    [Theory]
    [InlineData("Hello!")]
    [InlineData("")]
    public static void Encrypt_And_Decrypt_Return_Input(string plain)
    {
        var crypto = new Crypto(
            "secret",
            RandomNumberGenerator.GetBytes(Crypto.SaltBytes),
            Crypto.DefaultIterations);

        var plainBytes = Encoding.UTF8.GetBytes(plain);

        var encryptedBytes = crypto.Encrypt(
            RandomNumberGenerator.GetBytes(Crypto.NonceBytes),
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
            RandomNumberGenerator.GetBytes(Crypto.SaltBytes),
            Crypto.DefaultIterations);

        var otherCrypto = new Crypto(
            "other secret",
            RandomNumberGenerator.GetBytes(Crypto.SaltBytes),
            Crypto.DefaultIterations);

        var encryptedBytes = someCrypto.Encrypt(
            RandomNumberGenerator.GetBytes(Crypto.NonceBytes),
            Encoding.UTF8.GetBytes("Hello!"));

        Assert.Throws<AuthenticationTagMismatchException>(
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
                RandomNumberGenerator.GetBytes(Crypto.SaltBytes),
                Crypto.DefaultIterations));
    }
}
