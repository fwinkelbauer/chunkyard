namespace Chunkyard.Core;

/// <summary>
/// Contains methods to encrypt and decrypt data.
/// </summary>
public sealed class Crypto
{
    public const int DefaultIterations = 100_000;
    public const int NonceBytes = 12;
    public const int TagBytes = 16;
    public const int KeyBytes = 32;
    public const int SaltBytes = 12;

    private readonly byte[] _key;
    private readonly byte[] _hashedKey;

    public Crypto(string password, byte[] salt, int iterations)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException(
                "Password cannot be null or empty",
                nameof(password));
        }

        _key = PasswordToKey(password, salt, iterations);
        _hashedKey = SHA256.HashData(_key);

        Password = password;
        Salt = Convert.ToBase64String(salt);
        Iterations = iterations;
    }

    public Crypto(string password, string salt, int iterations)
        : this(password, Convert.FromBase64String(salt), iterations)
    {
    }

    public Crypto(string password)
        : this(password, RandomNumberGenerator.GetBytes(Crypto.SaltBytes), DefaultIterations)
    {
    }

    public string Password { get; }

    public string Salt { get; }

    public int Iterations { get; }

    public byte[] Encrypt(ReadOnlySpan<byte> plain)
    {
        var nonce = new Span<byte>(
            HMACSHA256.HashData(_hashedKey, plain),
            0,
            NonceBytes);

        var cipher = new byte[nonce.Length + plain.Length + TagBytes];

        nonce.CopyTo(new Span<byte>(cipher, 0, nonce.Length));

        var innerCipher = new Span<byte>(
            cipher,
            nonce.Length,
            plain.Length);

        var tag = new Span<byte>(
            cipher,
            cipher.Length - TagBytes,
            TagBytes);

        using var aesGcm = new AesGcm(_key, TagBytes);

        aesGcm.Encrypt(nonce, plain, innerCipher, tag);

        return cipher;
    }

    public byte[] Decrypt(ReadOnlySpan<byte> cipher)
    {
        var plainLength = cipher.Length - NonceBytes - TagBytes;
        var plain = new byte[plainLength];
        var nonce = cipher[..NonceBytes];
        var innerCipher = cipher.Slice(nonce.Length, plain.Length);
        var tag = cipher.Slice(cipher.Length - TagBytes, TagBytes);

        using var aesGcm = new AesGcm(_key, TagBytes);

        aesGcm.Decrypt(nonce, innerCipher, tag, plain);

        return plain;
    }

    private static byte[] PasswordToKey(
        string password,
        byte[] salt,
        int iterations)
    {
        using var rfc2898 = new Rfc2898DeriveBytes(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256);

        return rfc2898.GetBytes(KeyBytes);
    }
}
