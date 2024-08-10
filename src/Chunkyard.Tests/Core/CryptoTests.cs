namespace Chunkyard.Tests.Core;

[TestClass]
public sealed class CryptoTests
{
    [TestMethod]
    [DataRow("Hello!")]
    [DataRow("")]
    public void Encrypt_And_Decrypt_Return_Input(string input)
    {
        var crypto = Some.Crypto("secret");

        var plain = Encoding.UTF8.GetBytes(input);

        var encrypted = crypto.Encrypt(
            Some.World.GenerateNonce(),
            plain);

        var decrypted = crypto.Decrypt(encrypted);

        CollectionAssert.AreNotEqual(plain, encrypted);
        CollectionAssert.AreEqual(plain, decrypted);
    }

    [TestMethod]
    public void Decrypt_Throws_Given_Wrong_Key()
    {
        var someCrypto = Some.Crypto("some secret");
        var otherCrypto = Some.Crypto("other secret");

        var encrypted = someCrypto.Encrypt(
            Some.World.GenerateNonce(),
            "Hello!"u8);

        Assert.ThrowsException<AuthenticationTagMismatchException>(
            () => otherCrypto.Decrypt(encrypted));
    }

    [TestMethod]
    public void Constructor_Throws_On_EmptyPassword()
    {
        Assert.ThrowsException<ArgumentException>(
            () => Some.Crypto(""));
    }
}
