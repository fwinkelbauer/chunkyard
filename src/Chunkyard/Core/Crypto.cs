namespace Chunkyard.Core;

/// <summary>
/// Contains methods to encrypt and decrypt data.
/// </summary>
public sealed class Crypto : IDisposable
{
    public const int CryptoBytes = NonceBytes + TagBytes;

    private const int DefaultIterations = 1_000_000;
    private const int SaltBytes = 12;
    private const int NonceBytes = 12;
    private const int TagBytes = 16;
    private const int KeyBytes = 32;

    private readonly byte[] _hashedKey;
    private readonly AesGcm _aesGcm;

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

        var key = PasswordToKey(password, salt, iterations);
        _hashedKey = SHA256.HashData(key);
        _aesGcm = new AesGcm(key, TagBytes);

        Salt = Convert.ToBase64String(salt);
        Iterations = iterations;
    }

    public string Salt { get; }

    public int Iterations { get; }

    public void Dispose()
    {
        _aesGcm.Dispose();
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

        _aesGcm.Encrypt(nonce, plain, innerCipher, tag);

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
        var nonce = cipher[..NonceBytes];
        var innerCipher = cipher.Slice(nonce.Length, cipher.Length - CryptoBytes);
        var tag = cipher.Slice(cipher.Length - TagBytes, TagBytes);

        _aesGcm.Decrypt(nonce, innerCipher, tag, plain[..innerCipher.Length]);

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
