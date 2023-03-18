namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IPrompt"/> using the Linux application
/// secret-tool.
/// </summary>
internal sealed class SecretToolPrompt : IPrompt
{
    private const string SecretTool = "/usr/bin/secret-tool";

    private readonly string _repositoryPath;

    public SecretToolPrompt(string repositoryPath)
    {
        _repositoryPath = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(repositoryPath));
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
            $"lookup chunkyard-repository {_repositoryPath}",
            new[] { 0, 1 });
    }

    private void Store()
    {
        ProcessUtils.Run(
            SecretTool,
            $"store --label=\"Chunkyard\" chunkyard-repository {_repositoryPath}");
    }
}
