namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IPrompt"/> using the Linux application
/// secret-tool.
/// </summary>
internal class SecretToolPrompt : IPrompt
{
    private readonly string _repositoryPath;

    public SecretToolPrompt(string repositoryPath)
    {
        _repositoryPath = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(repositoryPath));
    }

    public string NewPassword()
    {
        if (!Installed())
        {
            return "";
        }

        if (string.IsNullOrEmpty(Lookup()))
        {
            Store();
        }

        return Lookup();
    }

    public string ExistingPassword()
    {
        return NewPassword();
    }

    private static bool Installed()
    {
        try
        {
            return !string.IsNullOrEmpty(
                ProcessUtils.RunQuery("which", "secret-tool", new[] { 0, 1 }));
        }
        catch (Exception)
        {
            return false;
        }
    }

    private string Lookup()
    {
        return ProcessUtils.RunQuery(
            "secret-tool",
            $"lookup chunkyard-repository {_repositoryPath}",
            new[] { 0, 1 });
    }

    private void Store()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "secret-tool",
            Arguments = $"store --label=\"Chunkyard\" chunkyard-repository {_repositoryPath}",
            UseShellExecute = true
        };

        ProcessUtils.Run(startInfo);
    }
}
