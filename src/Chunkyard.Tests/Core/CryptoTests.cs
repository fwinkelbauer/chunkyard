namespace Chunkyard.Tests.Core;

public static class CryptoTests
{
    [Theory]
    [InlineData("Hello!")]
    [InlineData("")]
    public static void Encrypt_And_Decrypt_Return_Input(string input)
    {
        var crypto = Some.Crypto("secret");

        var plain = Encoding.UTF8.GetBytes(input);

        var encrypted = crypto.Encrypt(
            Some.World.GenerateNonce(),
            plain);

        var decrypted = crypto.Decrypt(encrypted);

        Assert.NotEqual(plain, encrypted);
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public static void Decrypt_Throws_Given_Wrong_Key()
    {
        var someCrypto = Some.Crypto("some secret");
        var otherCrypto = Some.Crypto("other secret");

        var encrypted = someCrypto.Encrypt(
            Some.World.GenerateNonce(),
            "Hello!"u8);

        Assert.Throws<AuthenticationTagMismatchException>(
            () => otherCrypto.Decrypt(encrypted));
    }

    [Fact]
    public static void Constructor_Throws_On_EmptyPassword()
    {
        Assert.Throws<ArgumentException>(
            () => Some.Crypto(""));
    }
}
