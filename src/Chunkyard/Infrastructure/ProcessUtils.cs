namespace Chunkyard.Infrastructure;

/// <summary>
/// A set of process utility methods.
/// </summary>
public static class ProcessUtils
{
    public static void Run(
        ProcessStartInfo startInfo,
        int[]? validExitCodes = null,
        Action<string>? processOutput = null)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        using var process = Process.Start(startInfo);

        if (process == null)
        {
            throw new InvalidOperationException(
                $"Could not run '{startInfo.FileName}'");
        }

        if (processOutput != null)
        {
            if (startInfo.RedirectStandardOutput)
            {
                string? line;

                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    processOutput(line);
                }
            }

            if (startInfo.RedirectStandardError)
            {
                string? line;

                while ((line = process.StandardError.ReadLine()) != null)
                {
                    processOutput(line);
                }
            }
        }

        process.WaitForExit();

        validExitCodes ??= new[] { 0 };

        if (!validExitCodes.Contains(process.ExitCode))
        {
            throw new InvalidOperationException(
                $"Exit code of '{startInfo.FileName}' was {process.ExitCode}");
        }
    }

    public static void Run(
        string fileName,
        string arguments,
        int[]? validExitCodes = null)
    {
        Run(new ProcessStartInfo(fileName, arguments), validExitCodes);
    }

    public static string RunQuery(
        ProcessStartInfo startInfo,
        int[]? validExitCodes = null)
    {
        var builder = new StringBuilder();

        Run(
            startInfo,
            validExitCodes,
            line => builder.AppendLine(line));

        return builder.ToString().Trim();
    }

    public static string RunQuery(
        string fileName,
        string arguments,
        int[]? validExitCodes = null)
    {
        return RunQuery(
            new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true
            },
            validExitCodes);
    }
}
