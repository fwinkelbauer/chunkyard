namespace Chunkyard.Core;

/// <summary>
/// Contains methods to encrypt and decrypt data.
/// </summary>
public sealed class Crypto
{
    public const int CryptoBytes = NonceBytes + TagBytes;

    private const int DefaultIterations = 100_000;
    private const int SaltBytes = 12;
    private const int NonceBytes = 12;
    private const int TagBytes = 16;
    private const int KeyBytes = 32;

    private readonly byte[] _key;
    private readonly byte[] _hashedKey;

    public Crypto(string password, string salt, int iterations)
        : this(password, Convert.FromBase64String(salt), iterations)
    {
    }

    public Crypto(string password)
        : this(password, RandomNumberGenerator.GetBytes(SaltBytes), DefaultIterations)
    {
    }

    private Crypto(string password, byte[] salt, int iterations)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password cannot be null or empty");
        }

        _key = PasswordToKey(password, salt, iterations);
        _hashedKey = SHA256.HashData(_key);

        Salt = Convert.ToBase64String(salt);
        Iterations = iterations;
    }

    public string Salt { get; }

    public int Iterations { get; }

    public static void Deconstruct(
        ReadOnlySpan<byte> cipher,
        out ReadOnlySpan<byte> nonce,
        out ReadOnlySpan<byte> innerCipher,
        out ReadOnlySpan<byte> tag)
    {
        nonce = cipher[..NonceBytes];
        innerCipher = cipher.Slice(nonce.Length, cipher.Length - CryptoBytes);
        tag = cipher.Slice(cipher.Length - TagBytes, TagBytes);
    }

    public byte[] Encrypt(ReadOnlySpan<byte> plain)
    {
        var cipher = new byte[plain.Length + CryptoBytes];

        Encrypt(plain, cipher);

        return cipher;
    }

    public ReadOnlySpan<byte> Encrypt(ReadOnlySpan<byte> plain, Span<byte> cipher)
    {
        var nonce = HMACSHA256.HashData(_hashedKey, plain)
            .AsSpan(0, NonceBytes);

        nonce.CopyTo(cipher[..nonce.Length]);

        var innerCipher = cipher.Slice(nonce.Length, plain.Length);
        var tag = cipher.Slice(nonce.Length + plain.Length, TagBytes);

        using var aesGcm = new AesGcm(_key, TagBytes);

        aesGcm.Encrypt(nonce, plain, innerCipher, tag);

        return cipher[..(plain.Length + CryptoBytes)];
    }

    public byte[] Decrypt(ReadOnlySpan<byte> cipher)
    {
        var plain = new byte[cipher.Length - CryptoBytes];

        Decrypt(cipher, plain);

        return plain;
    }

    public ReadOnlySpan<byte> Decrypt(ReadOnlySpan<byte> cipher, Span<byte> plain)
    {
        Deconstruct(cipher, out var nonce, out var innerCipher, out var tag);

        using var aesGcm = new AesGcm(_key, TagBytes);

        aesGcm.Decrypt(nonce, innerCipher, tag, plain[..innerCipher.Length]);

        return plain[..innerCipher.Length];
    }

    private static byte[] PasswordToKey(
        string password,
        byte[] salt,
        int iterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            KeyBytes);
    }
}
