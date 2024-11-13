namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IPrompt"/> using the Linux library
/// libsecret.
/// </summary>
internal sealed class LibsecretPrompt : IPrompt
{
    private static readonly IntPtr Schema = secret_schema_new(
        "chunkyard.libsecret",
        1 << 1,
        "chunkyard-repository",
        0,
        IntPtr.Zero);

    private readonly IPrompt _prompt;

    public LibsecretPrompt(IPrompt prompt)
    {
        _prompt = prompt;
    }

    public string NewPassword(string key)
    {
        EnsureLinux();

        return Store(key, _prompt.NewPassword(key));
    }

    public string ExistingPassword(string key)
    {
        EnsureLinux();

        return TryRetrieve(key, out var password)
            ? password
            : Store(key, _prompt.ExistingPassword(key));
    }

    private static void EnsureLinux()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            throw new NotSupportedException(
                "The libsecret prompt is only available on Linux");
        }
    }

    private static bool TryRetrieve(string key, out string password)
    {
        password = secret_password_lookup_sync(
            Schema,
            IntPtr.Zero,
            out var error,
            "chunkyard-repository",
            key,
            IntPtr.Zero);

        if (error != IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Could not read from libsecret");
        }

        return !string.IsNullOrEmpty(password);
    }

    private static string Store(string key, string password)
    {
        _ = secret_password_store_sync(
            Schema,
            "default",
            $"Chunkyard: {key}",
            password,
            IntPtr.Zero,
            out var error,
            "chunkyard-repository",
            key,
            IntPtr.Zero);

        if (error != IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Could not write to libsecret");
        }

        return password;
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
