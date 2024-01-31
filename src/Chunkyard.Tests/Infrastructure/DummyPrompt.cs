namespace Chunkyard.Tests.Infrastructure;

internal sealed class DummyPrompt : IPrompt
{
    private readonly string _password;

    public DummyPrompt(string password, int iterations)
    {
        _password = password;

        Iterations = iterations;
    }

    public int Iterations { get; }

    public string NewPassword()
    {
        return _password;
    }

    public string ExistingPassword()
    {
        return _password;
    }
}
