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
            var startInfo = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = "secret-tool",
                RedirectStandardOutput = true
            };

            return !string.IsNullOrEmpty(
                ProcessUtils.RunQuery(startInfo, new[] { 0, 1 }));
        }
        catch (Exception)
        {
            return false;
        }
    }

    private string Lookup()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "secret-tool",
            Arguments = $"lookup chunkyard-repository {_repositoryPath}",
            RedirectStandardOutput = true
        };

        return ProcessUtils.RunQuery(startInfo, new[] { 0, 1 });
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
