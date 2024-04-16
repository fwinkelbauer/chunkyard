namespace Chunkyard.Tests.Infrastructure;

internal sealed class DummyPrompt : IPrompt
{
    private static readonly Dictionary<string, string> _passwords = new();

    private readonly string _password;

    public DummyPrompt(string password)
    {
        _password = password;
    }

    public string NewPassword(string key)
    {
        _passwords[key] = _password;

        return _password;
    }

    public string ExistingPassword(string key)
    {
        return _passwords[key];
    }
}
