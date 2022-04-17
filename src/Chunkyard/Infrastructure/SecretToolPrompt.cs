namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IPrompt"/> using the Linux application
/// secret-tool.
/// </summary>
internal class SecretToolPrompt : IPrompt
{
    private const string PromptVariable = "CHUNKYARD_PROMPT";

    private readonly string _repositoryPath;

    public SecretToolPrompt(string repositoryPath)
    {
        _repositoryPath = Path.GetFullPath(repositoryPath);
    }

    public string? NewPassword()
    {
        var promptName = Environment.GetEnvironmentVariable(PromptVariable);

        if (string.IsNullOrEmpty(promptName)
            || !promptName.Equals("secret-tool"))
        {
            return null;
        }

        if (string.IsNullOrEmpty(Lookup()))
        {
            Store();
        }

        return Lookup();
    }

    public string? ExistingPassword()
    {
        return NewPassword();
    }

    private string Lookup()
    {
        var fileName = "secret-tool";
        var arguments = $"lookup chunkyard-repository {_repositoryPath}";
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true
        };

        return ProcessUtils.RunQuery(startInfo);
    }

    private void Store()
    {
        var fileName = "secret-tool";
        var arguments = $"store --label=\"Chunkyard\" chunkyard-repository {_repositoryPath}";
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = true
        };

        ProcessUtils.Run(startInfo);
    }
}
