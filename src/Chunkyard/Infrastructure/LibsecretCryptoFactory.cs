namespace Chunkyard.Infrastructure;

internal sealed class LibsecretCryptoFactory : ICryptoFactory
{
    private static readonly IntPtr Schema = secret_schema_new(
        "chunkyard.libsecret",
        1 << 1,
        "chunkyard-repository",
        0,
        IntPtr.Zero);

    private readonly ICryptoFactory _cryptoFactory;

    public LibsecretCryptoFactory(ICryptoFactory cryptoFactory)
    {
        _cryptoFactory = cryptoFactory;
    }

    public Crypto Create(SnapshotReference? snapshotReference)
    {
        EnsureLinux();

        if (snapshotReference != null
            && TryRetrieve(snapshotReference, out var password))
        {
            return new Crypto(
                password,
                snapshotReference.Salt,
                snapshotReference.Iterations);
        }
        else
        {
            return Store(_cryptoFactory.Create(snapshotReference));
        }
    }

    private static void EnsureLinux()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            throw new NotSupportedException(
                "Libsecret is only available on Linux");
        }
    }

    private static string ToKeyId(string salt, int iterations)
    {
        var saltText = Convert.ToHexString(Convert.FromBase64String(salt))
            .ToLowerInvariant();

        return $"s-{saltText}-i-{iterations}";
    }

    private static bool TryRetrieve(
        SnapshotReference snapshotReference,
        out string password)
    {
        var keyId = ToKeyId(
            snapshotReference.Salt,
            snapshotReference.Iterations);

        password = secret_password_lookup_sync(
            Schema,
            IntPtr.Zero,
            out var error,
            "chunkyard-repository",
            keyId,
            IntPtr.Zero);

        if (error != IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Could not read from libsecret");
        }

        return !string.IsNullOrEmpty(password);
    }

    private static Crypto Store(Crypto crypto)
    {
        var keyId = ToKeyId(crypto.Salt, crypto.Iterations);

        _ = secret_password_store_sync(
            Schema,
            "default",
            $"Chunkyard: {keyId}",
            crypto.Password,
            IntPtr.Zero,
            out var error,
            "chunkyard-repository",
            keyId,
            IntPtr.Zero);

        if (error != IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Could not write to libsecret");
        }

        return crypto;
    }

    [DllImport("libsecret-1.so.0", CallingConvention = CallingConvention.StdCall)]
    private static extern int secret_password_store_sync(
        IntPtr schema,
        string collection,
        string label,
        string password,
        IntPtr cancellable,
        out IntPtr error,
        string attributeType,
        string attributeValue,
        IntPtr end);


    [DllImport("libsecret-1.so.0", CallingConvention = CallingConvention.StdCall)]
    private static extern string secret_password_lookup_sync(
        IntPtr schema,
        IntPtr cancellable,
        out IntPtr error,
        string attributeType,
        string attributeValue,
        IntPtr end);

    [DllImport("libsecret-1.so.0", CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr secret_schema_new(
        string name,
        int flags,
        string attribute,
        int attributeType,
        IntPtr end);
}
