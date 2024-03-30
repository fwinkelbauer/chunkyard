namespace Chunkyard.Tests.Core;

public static class CryptoTests
{
    [Theory]
    [InlineData("Hello!")]
    [InlineData("")]
    public static void Encrypt_And_Decrypt_Return_Input(string plain)
    {
        var crypto = Some.Crypto("secret");

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
        var someCrypto = Some.Crypto("some secret");
        var otherCrypto = Some.Crypto("other secret");

        var encryptedBytes = someCrypto.Encrypt(
            RandomNumberGenerator.GetBytes(Crypto.NonceBytes),
            "Hello!"u8);

        Assert.Throws<AuthenticationTagMismatchException>(
            () => otherCrypto.Decrypt(encryptedBytes));
    }

    [Fact]
    public static void Constructor_Throws_On_EmptyPassword()
    {
        Assert.Throws<ArgumentException>(
            () => Some.Crypto(""));
    }
}
