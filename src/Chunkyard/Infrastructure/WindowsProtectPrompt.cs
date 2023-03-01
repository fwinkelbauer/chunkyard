namespace Chunkyard.Infrastructure;

internal sealed class WindowsProtectPrompt : IPrompt
{
    private readonly IPrompt _prompt;
    private readonly IRepository<string> _repository;
    private readonly byte[] _entropy;
    private readonly string _key;

    public WindowsProtectPrompt(string repositoryPath)
    {
        _prompt = new ConsolePrompt();

        _repository = new FileRepository<string>(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".chunkyard-cred"),
            key => key,
            Path.GetFileName);

        _entropy = SHA256.HashData(
            Encoding.UTF8.GetBytes(repositoryPath));

        _key = Convert.ToHexString(_entropy)
            .ToLowerInvariant();
    }

    public string NewPassword()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "";
        }

        byte[]? protectedBytes;

        if (_repository.Exists(_key))
        {
            protectedBytes = _repository.Retrieve(_key);
        }
        else
        {
            protectedBytes = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(_prompt.NewPassword()),
                _entropy,
                DataProtectionScope.CurrentUser);

            _repository.Store(_key, protectedBytes);
        }

        var unprotectedBytes = ProtectedData.Unprotect(
            protectedBytes,
            _entropy,
            DataProtectionScope.CurrentUser);

        return Encoding.UTF8.GetString(unprotectedBytes);
    }

    public string ExistingPassword()
    {
        return NewPassword();
    }
}
