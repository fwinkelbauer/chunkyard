namespace Chunkyard.Tests.Core;

[TestClass]
public sealed class CryptoTests
{
    [TestMethod]
    [DataRow("Hello!")]
    [DataRow("")]
    public void Encrypt_And_Decrypt_Return_Input(string input)
    {
        var crypto = Some.Crypto();

        var plain = Encoding.UTF8.GetBytes(input);
        var encrypted = crypto.Encrypt(plain);
        var decrypted = crypto.Decrypt(encrypted);

        CollectionAssert.AreNotEqual(plain, encrypted);
        CollectionAssert.AreEqual(plain, decrypted);
    }

    [TestMethod]
    public void Decrypt_Throws_Given_Wrong_Key()
    {
        var someCrypto = Some.Crypto("some secret");
        var otherCrypto = Some.Crypto("other secret");

        var encrypted = someCrypto.Encrypt("Hello!"u8);

        _ = Assert.Throws<AuthenticationTagMismatchException>(
            () => otherCrypto.Decrypt(encrypted));
    }

    [TestMethod]
    public void Encrypt_Uses_Different_Nonce_On_Every_Input()
    {
        var crypto = Some.Crypto();
        var plain = "Hello!"u8;

        CollectionAssert.AreNotEqual(
            crypto.Encrypt(plain),
            crypto.Encrypt(plain));
    }

    [TestMethod]
    public void Constructor_Throws_On_EmptyPassword()
    {
        _ = Assert.Throws<ArgumentException>(
            () => Some.Crypto(""));
    }
}
