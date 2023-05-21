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
    private readonly string _repositoryId;

    public LibsecretPrompt(
        IPrompt prompt,
        string repositoryId)
    {
        _prompt = prompt;
        _repositoryId = repositoryId;
    }

    public string NewPassword()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            throw new NotSupportedException(
                "The libsecret prompt is only available on Linux");
        }

        var password = Lookup();

        if (string.IsNullOrEmpty(password))
        {
            return Store();
        }
        else
        {
            return password;
        }
    }

    public string ExistingPassword()
    {
        return NewPassword();
    }

    private string Lookup()
    {
        var password = secret_password_lookup_sync(
            Schema,
            IntPtr.Zero,
            out var error,
            "chunkyard-repository",
            _repositoryId,
            IntPtr.Zero);

        if (error != IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Could not read from libsecret");
        }

        return password;
    }

    private string Store()
    {
        var password = _prompt.NewPassword();

        secret_password_store_sync(
            Schema,
            "default",
            $"Chunkyard {_repositoryId}",
            password,
            IntPtr.Zero,
            out var error,
            "chunkyard-repository",
            _repositoryId,
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
