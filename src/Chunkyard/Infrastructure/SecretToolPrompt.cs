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

    public string? NewPassword()
    {
        if (!Installed())
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

    private static bool Installed()
    {
        try
        {
            var startInfo = new ProcessStartInfo("which", "secret-tool")
            {
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
        var fileName = "secret-tool";
        var arguments = $"lookup chunkyard-repository {_repositoryPath}";
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true
        };

        return ProcessUtils.RunQuery(startInfo, new[] { 0, 1 });
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
