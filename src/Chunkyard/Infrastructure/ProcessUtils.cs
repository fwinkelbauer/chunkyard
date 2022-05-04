namespace Chunkyard.Infrastructure;

/// <summary>
/// A set of process utility methods.
/// </summary>
public static class ProcessUtils
{
    public static void Run(
        ProcessStartInfo startInfo,
        int[]? validExitCodes = null)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        using var process = Start(startInfo);

        EnsureValidExitCode(process, validExitCodes);
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
        ArgumentNullException.ThrowIfNull(startInfo);

        using var process = Start(startInfo);

        var builder = new StringBuilder();
        string? line;

        while ((line = process.StandardOutput.ReadLine()) != null)
        {
            builder.Append(line);
        }

        EnsureValidExitCode(process, validExitCodes);

        return builder.ToString();
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

    private static void EnsureValidExitCode(
        Process process,
        int[]? validExitCodes)
    {
        process.WaitForExit();

        validExitCodes ??= new[] { 0 };

        if (!validExitCodes.Contains(process.ExitCode))
        {
            throw new InvalidOperationException(
                $"Exit code of '{process.StartInfo.FileName}' was {process.ExitCode}");
        }
    }

    private static Process Start(ProcessStartInfo startInfo)
    {
        var process = Process.Start(startInfo);

        if (process == null)
        {
            throw new InvalidOperationException(
                $"Could not run '{startInfo.FileName}'");
        }

        return process;
    }
}
