namespace Chunkyard.Build;

/// <summary>
/// A set of process utility methods.
/// </summary>
internal static class ProcessUtils
{
    public static void Run(
        ProcessStartInfo startInfo,
        int[]? validExitCodes = null)
    {
        using var process = Start(startInfo);

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

    public static void RunQuery(
        ProcessStartInfo startInfo,
        Action<string> processOutput,
        int[]? validExitCodes = null)
    {
        using var process = Start(startInfo);
        string? line;

        while ((line = process.StandardOutput.ReadLine()) != null)
        {
            processOutput(line);
        }

        process.WaitForExit();

        validExitCodes ??= new[] { 0 };

        if (!validExitCodes.Contains(process.ExitCode))
        {
            throw new InvalidOperationException(
                $"Exit code of '{startInfo.FileName}' was {process.ExitCode}");
        }
    }

    public static string RunQuery(
        string fileName,
        string arguments,
        int[]? validExitCodes = null)
    {
        var builder = new StringBuilder();

        RunQuery(
            new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true
            },
            line => builder.AppendLine(line),
            validExitCodes);

        return builder.ToString();
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
