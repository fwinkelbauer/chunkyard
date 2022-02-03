namespace Chunkyard.Core;

/// <summary>
/// Contains methods to encrypt and decrypt data.
/// </summary>
public class AesGcmCrypto
{
    public const int DefaultIterations = 1000;
    public const int EncryptionBytes = NonceBytes + TagBytes;

    private const int NonceBytes = 12;
    private const int TagBytes = 16;
    private const int KeyBytes = 32;
    private const int SaltBytes = 12;

    private readonly byte[] _key;

    public AesGcmCrypto(string password, byte[] salt, int iterations)
    {
        _key = PasswordToKey(password, salt, iterations);

        Salt = salt;
        Iterations = iterations;
    }

    public byte[] Salt { get; }

    public int Iterations { get; }

    public int Encrypt(
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> plainText,
        byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        nonce.CopyTo(
            new Span<byte>(buffer, 0, nonce.Length));

        var ciphertext = new Span<byte>(
            buffer,
            nonce.Length,
            plainText.Length);

        var tag = new Span<byte>(
            buffer,
            nonce.Length + ciphertext.Length,
            TagBytes);

        using var aesGcm = new AesGcm(_key);
        aesGcm.Encrypt(nonce, plainText, ciphertext, tag);

        return nonce.Length + plainText.Length + TagBytes;
    }

    public byte[] Decrypt(
        ReadOnlySpan<byte> cipherText)
    {
        var plainText = new byte[
            cipherText.Length - NonceBytes - TagBytes];

        var nonce = cipherText[..NonceBytes];

        var innerCipherText = cipherText.Slice(
            nonce.Length,
            plainText.Length);

        var tag = cipherText.Slice(
            cipherText.Length - TagBytes,
            TagBytes);

        using var aesGcm = new AesGcm(_key);

        aesGcm.Decrypt(nonce, innerCipherText, tag, plainText);

        return plainText;
    }

    private static byte[] PasswordToKey(
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
        return RandomNumberGenerator.GetBytes(SaltBytes);
    }

    public static byte[] GenerateNonce()
    {
        return RandomNumberGenerator.GetBytes(NonceBytes);
    }
}
