namespace Chunkyard.Build;

/// <summary>
/// A set of process related utility methods.
/// </summary>
public static class ProcessUtils
{
    public static void Run(string fileName, string arguments)
    {
        using var process = Process.Start(fileName, arguments)!;

        WaitForSuccess(process);
    }

    public static string RunQuery(string fileName, string arguments)
    {
        using var process = Process.Start(
            new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true
            })!;

        var builder = new StringBuilder();
        string? line;

        while ((line = process.StandardOutput.ReadLine()) != null)
        {
            builder.AppendLine(line);
        }

        WaitForSuccess(process);

        return builder.ToString().TrimEnd();
    }

    private static void WaitForSuccess(Process process)
    {
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Exit code of '{process.StartInfo.FileName}' was {process.ExitCode}");
        }
    }
}
