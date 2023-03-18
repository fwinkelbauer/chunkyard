namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IPrompt"/> using the Linux application
/// secret-tool.
/// </summary>
internal sealed class SecretToolPrompt : IPrompt
{
    private const string SecretTool = "/usr/bin/secret-tool";

    private readonly string _repositoryId;

    public SecretToolPrompt(string repositoryId)
    {
        _repositoryId = repositoryId;
    }

    public string NewPassword()
    {
        if (!File.Exists(SecretTool))
        {
            return "";
        }

        var password = Lookup();

        if (string.IsNullOrEmpty(password))
        {
            Store();

            return Lookup();
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
        return ProcessUtils.RunQuery(
            SecretTool,
            $"lookup chunkyard-repository {_repositoryId}",
            new[] { 0, 1 });
    }

    private void Store()
    {
        ProcessUtils.Run(
            SecretTool,
            $"store --label=\"Chunkyard\" chunkyard-repository {_repositoryId}");
    }
}
