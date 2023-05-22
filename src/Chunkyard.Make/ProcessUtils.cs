namespace Chunkyard.Make;

/// <summary>
/// A set of process utility methods.
/// </summary>
public static class ProcessUtils
{
    public static void Run(
        ProcessStartInfo startInfo,
        Func<int, bool>? isValidExitCodes = null,
        Action<string>? processOutput = null)
    {
        using var process = Process.Start(startInfo);

        if (process == null)
        {
            return;
        }

        if (processOutput != null)
        {
            string? line;

            if (startInfo.RedirectStandardOutput)
            {
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    processOutput(line);
                }
            }

            if (startInfo.RedirectStandardError)
            {
                while ((line = process.StandardError.ReadLine()) != null)
                {
                    processOutput(line);
                }
            }
        }

        process.WaitForExit();

        isValidExitCodes ??= exitCode => exitCode == 0;

        if (!isValidExitCodes(process.ExitCode))
        {
            throw new InvalidOperationException(
                $"Exit code of '{startInfo.FileName}' was {process.ExitCode}");
        }
    }

    public static void Run(
        string fileName,
        string arguments,
        Func<int, bool>? isValidExitCodes = null)
    {
        Run(new ProcessStartInfo(fileName, arguments), isValidExitCodes);
    }

    public static string RunQuery(
        ProcessStartInfo startInfo,
        Func<int, bool>? isValidExitCodes = null)
    {
        var builder = new StringBuilder();

        Run(
            startInfo,
            isValidExitCodes,
            line => builder.AppendLine(line));

        return builder.ToString().TrimEnd();
    }

    public static string RunQuery(
        string fileName,
        string arguments,
        Func<int, bool>? isValidExitCodes = null)
    {
        return RunQuery(
            new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true
            },
            isValidExitCodes);
    }
}
