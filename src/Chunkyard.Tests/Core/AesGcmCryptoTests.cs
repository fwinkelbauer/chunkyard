namespace Chunkyard.Tests.Core;

public static class AesGcmCryptoTests
{
    [Fact]
    public static void Encrypt_And_Decrypt_Return_Input()
    {
        var plainBytes = Encoding.UTF8.GetBytes("Hello!");
        var key = AesGcmCrypto.PasswordToKey(
            "secret",
            AesGcmCrypto.GenerateSalt(),
            AesGcmCrypto.Iterations);

        var encryptedBytes = AesGcmCrypto.Encrypt(
            AesGcmCrypto.GenerateNonce(),
            plainBytes,
            key);

        var decryptedBytes = AesGcmCrypto.Decrypt(encryptedBytes, key);

        Assert.NotEqual(plainBytes, encryptedBytes);
        Assert.Equal(plainBytes, decryptedBytes);
    }

    [Fact]
    public static void Decrypt_Throws_Given_Wrong_Key()
    {
        var someKey = AesGcmCrypto.PasswordToKey(
            "some secret",
            AesGcmCrypto.GenerateSalt(),
            AesGcmCrypto.Iterations);

        var otherKey = AesGcmCrypto.PasswordToKey(
            "other secret",
            AesGcmCrypto.GenerateSalt(),
            AesGcmCrypto.Iterations);

        var encryptedBytes = AesGcmCrypto.Encrypt(
            AesGcmCrypto.GenerateNonce(),
            Encoding.UTF8.GetBytes("Hello!"),
            someKey);

        Assert.Throws<CryptographicException>(
            () => AesGcmCrypto.Decrypt(encryptedBytes, otherKey));
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
