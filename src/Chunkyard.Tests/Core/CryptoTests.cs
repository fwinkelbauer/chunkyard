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
        var encrypted = crypto.Encrypt(plain);
        var decrypted = crypto.Decrypt(encrypted);

        Assert.NotEqual(plain, encrypted);
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public static void Decrypt_Throws_Given_Wrong_Key()
    {
        var someCrypto = Some.Crypto("some secret");
        var otherCrypto = Some.Crypto("other secret");

        var encrypted = someCrypto.Encrypt("Hello!"u8);

        Assert.Throws<AuthenticationTagMismatchException>(
            () => otherCrypto.Decrypt(encrypted));
    }

    [Fact]
    public static void Encrypt_Uses_Different_Nonce_On_Every_Input()
    {
        var crypto = Some.Crypto("secret");
        var plain = "Hello!"u8;

        Assert.NotEqual(
            crypto.Encrypt(plain),
            crypto.Encrypt(plain));
    }

    [Fact]
    public static void Constructor_Throws_On_EmptyPassword()
    {
        Assert.Throws<ArgumentException>(
            () => Some.Crypto(""));
    }
}
