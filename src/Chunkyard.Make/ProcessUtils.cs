namespace Chunkyard.Make;

/// <summary>
/// A set of process utility methods.
/// </summary>
public static class ProcessUtils
{
    public static void Run(
        ProcessStartInfo startInfo,
        Func<int, bool>? isValidExitCode = null,
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

        isValidExitCode ??= exitCode => exitCode == 0;

        if (!isValidExitCode(process.ExitCode))
        {
            throw new InvalidOperationException(
                $"Exit code of '{startInfo.FileName}' was {process.ExitCode}");
        }
    }

    public static void Run(
        string fileName,
        string arguments,
        Func<int, bool>? isValidExitCode = null)
    {
        Run(new ProcessStartInfo(fileName, arguments), isValidExitCode);
    }

    public static string RunQuery(
        ProcessStartInfo startInfo,
        Func<int, bool>? isValidExitCode = null)
    {
        var builder = new StringBuilder();

        Run(
            startInfo,
            isValidExitCode,
            line => builder.AppendLine(line));

        return builder.ToString().TrimEnd();
    }

    public static string RunQuery(
        string fileName,
        string arguments,
        Func<int, bool>? isValidExitCode = null)
    {
        return RunQuery(
            new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true
            },
            isValidExitCode);
    }
}
