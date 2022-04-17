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

        using var process = Process.Start(startInfo);

        if (process == null)
        {
            throw new ChunkyardException(
                $"Could not run '{fileName}'");
        }

        var builder = new StringBuilder();
        string? line;

        while ((line = process.StandardOutput.ReadLine()) != null)
        {
            builder.Append(line);
        }

        process.WaitForExit();

        if (process.ExitCode != 0 && process.ExitCode != 1)
        {
            throw new ChunkyardException(
                $"Exit code of '{fileName}' was {process.ExitCode}");
        }

        return builder.ToString();
    }

    private void Store()
    {
        var fileName = "secret-tool";
        var arguments = $"store --label=\"Chunkyard {_repositoryPath}\" chunkyard-repository {_repositoryPath}";
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = true
        };

        using var process = Process.Start(startInfo);

        if (process == null)
        {
            throw new ChunkyardException(
                $"Could not run '{fileName}'");
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new ChunkyardException(
                $"Exit code of '{fileName}' was {process.ExitCode}");
        }
    }
}
