namespace Chunkyard.Infrastructure;

/// <summary>
/// A set of process utility methods.
/// </summary>
internal static class ProcessUtils
{
    public static void Run(ProcessStartInfo startInfo)
    {
        using var process = Process.Start(startInfo);

        if (process == null)
        {
            throw new ChunkyardException(
                $"Could not run '{startInfo.FileName}'");
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new ChunkyardException(
                $"Exit code of '{startInfo.FileName}' was {process.ExitCode}");
        }
    }

    public static string RunQuery(ProcessStartInfo startInfo)
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

        if (process.ExitCode != 0)
        {
            throw new ChunkyardException(
                $"Exit code of '{startInfo.FileName}' was {process.ExitCode}");
        }

        return builder.ToString();
    }
}
