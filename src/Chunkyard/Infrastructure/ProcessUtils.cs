namespace Chunkyard.Infrastructure;

/// <summary>
/// A set of process utility methods.
/// </summary>
internal static class ProcessUtils
{
    public static void Run(
        ProcessStartInfo startInfo,
        int[]? validExitCodes = null)
    {
        using var process = Process.Start(startInfo);

        if (process == null)
        {
            throw new ChunkyardException(
                $"Could not run '{startInfo.FileName}'");
        }

        process.WaitForExit();

        validExitCodes ??= new[] { 0 };

        if (!validExitCodes.Contains(process.ExitCode))
        {
            throw new ChunkyardException(
                $"Exit code of '{startInfo.FileName}' was {process.ExitCode}");
        }
    }

    public static string RunQuery(
        ProcessStartInfo startInfo,
        int[]? validExitCodes = null)
    {
        using var process = Process.Start(startInfo);

        if (process == null)
        {
            throw new ChunkyardException(
                $"Could not run '{startInfo.FileName}'");
        }

        var builder = new StringBuilder();
        string? line;

        while ((line = process.StandardOutput.ReadLine()) != null)
        {
            builder.Append(line);
        }

        process.WaitForExit();

        validExitCodes ??= new[] { 0 };

        if (!validExitCodes.Contains(process.ExitCode))
        {
            throw new ChunkyardException(
                $"Exit code of '{startInfo.FileName}' was {process.ExitCode}");
        }

        return builder.ToString();
    }
}
