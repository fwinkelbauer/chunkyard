namespace Chunkyard.Infrastructure;

/// <summary>
/// An <see cref="IPrompt"/> which retrieves a password from a set of
/// environment variables.
/// </summary>
internal class EnvironmentPrompt : IPrompt
{
    private const string PasswordVariable = "CHUNKYARD_PASSWORD";
    private const string ProcessVariable = "CHUNKYARD_PASSCMD";

    public string? NewPassword()
    {
        return Environment.GetEnvironmentVariable(PasswordVariable)
            ?? GetProcessPassword();
    }

    public string? ExistingPassword()
    {
        return NewPassword();
    }

    private static string? GetProcessPassword()
    {
        var command = Environment.GetEnvironmentVariable(ProcessVariable);

        if (string.IsNullOrEmpty(command))
        {
            return null;
        }

        var split = command.Split(" ", 2);
        var fileName = split[0];
        var arguments = split.Length > 1
            ? split[1]
            : "";

        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true
        };

        using var process = Process.Start(startInfo);

        if (process == null)
        {
            throw new ChunkyardException(
                $"Could not run '{command}'");
        }

        var builder = new StringBuilder();
        string? line;

        while ((line = process.StandardOutput.ReadLine()) != null)
        {
            builder.Append(line);
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new ChunkyardException(
                $"Exit code of '{command}' was {process.ExitCode}");
        }

        return builder.ToString();
    }
}
