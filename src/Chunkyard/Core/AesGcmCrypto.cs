namespace Chunkyard.Core;

/// <summary>
/// Contains methods to encrypt and decrypt data.
/// </summary>
public static class AesGcmCrypto
{
    public const int Iterations = 1000;

    private const int NonceBytes = 12;
    private const int TagBytes = 16;
    private const int KeyBytes = 32;
    private const int SaltBytes = 12;

    public static byte[] Encrypt(
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> plainText,
        ReadOnlySpan<byte> key)
    {
        var cipherText = new byte[
            nonce.Length + plainText.Length + TagBytes];

        nonce.CopyTo(
            new Span<byte>(cipherText, 0, nonce.Length));

        var innerCiphertext = new Span<byte>(
            cipherText,
            nonce.Length,
            plainText.Length);

        var tag = new Span<byte>(
            cipherText,
            cipherText.Length - TagBytes,
            TagBytes);

        using var aesGcm = new AesGcm(key);
        aesGcm.Encrypt(nonce, plainText, innerCiphertext, tag);

        return cipherText;
    }

    public static byte[] Decrypt(
        ReadOnlySpan<byte> cipherText,
        ReadOnlySpan<byte> key)
    {
        var plainText = new byte[
            cipherText.Length - NonceBytes - TagBytes];

        var nonce = cipherText.Slice(0, NonceBytes);

        var innerCipherText = cipherText.Slice(
            nonce.Length,
            plainText.Length);

        var tag = cipherText.Slice(
            cipherText.Length - TagBytes,
            TagBytes);

        using var aesGcm = new AesGcm(key);

        aesGcm.Decrypt(nonce, innerCipherText, tag, plainText);

        return plainText;
    }

    public static byte[] PasswordToKey(
        string password,
        byte[] salt,
        int iterations)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException(
                "Password cannot be empty");
        }

        using var rfc2898 = new Rfc2898DeriveBytes(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256);

        return rfc2898.GetBytes(KeyBytes);
    }

    public static byte[] GenerateSalt()
    {
        return GenerateRandomNumber(SaltBytes);
    }

    public static byte[] GenerateNonce()
    {
        return GenerateRandomNumber(NonceBytes);
    }

    private static byte[] GenerateRandomNumber(int length)
    {
        using var randomGenerator = RandomNumberGenerator.Create();
        var randomNumber = new byte[length];
        randomGenerator.GetBytes(randomNumber);

        return randomNumber;
    }
}
