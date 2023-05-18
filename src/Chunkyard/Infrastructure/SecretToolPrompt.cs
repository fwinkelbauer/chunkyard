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
            throw new InvalidOperationException(
                $"Could not find {SecretTool}. Install the application or try a different prompt");
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
        var startInfo = new ProcessStartInfo(
            SecretTool,
            $"lookup chunkyard-repository {_repositoryId}")
        {
            RedirectStandardOutput = true
        };

        using var process = Process.Start(startInfo)!;

        string? line;
        var builder = new StringBuilder();

        while ((line = process.StandardOutput.ReadLine()) != null)
        {
            builder.AppendLine(line);
        }

        process.WaitForExit();

        return builder.ToString().TrimEnd();
    }

    private void Store()
    {
        using var process = Process.Start(
            SecretTool,
            $"store --label=\"Chunkyard\" chunkyard-repository {_repositoryId}");

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Could not store password using {SecretTool}");
        }
    }
}
