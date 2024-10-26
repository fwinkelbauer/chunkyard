namespace Chunkyard.Tests.Infrastructure;

internal sealed class DummyPrompt : IPrompt
{
    private static readonly Dictionary<string, string> Passwords = new();

    private readonly string _password;

    public DummyPrompt(string password)
    {
        _password = password;
    }

    public string NewPassword(string key)
    {
        Passwords[key] = _password;

        return _password;
    }

    public string ExistingPassword(string key)
    {
        return Passwords[key];
    }
}
