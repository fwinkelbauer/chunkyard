namespace Chunkyard.Build;

internal static class BuildUtils
{
    public static string RunQuery(
        string fileName,
        string[] arguments,
        int[] validExitCodes)
    {
        var builder = new StringBuilder();

        Run(
            fileName,
            arguments,
            validExitCodes,
            line => builder.AppendLine(line));

        return builder.ToString().TrimEnd();
    }

    public static void Run(
        string fileName,
        string[] arguments,
        int[] validExitCodes,
        Action<string>? processOutput = null)
    {
        var startInfo = new ProcessStartInfo(
            fileName,
            string.Join(' ', arguments))
        {
            RedirectStandardOutput = processOutput != null
        };

        using var process = Process.Start(startInfo);

        if (process == null)
        {
            throw new BuildException(
                $"Could not start process '{fileName}'");
        }

        string? line;

        if (processOutput != null)
        {
            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                processOutput(line);
            }
        }

        process.WaitForExit();

        if (!validExitCodes.Contains(process.ExitCode))
        {
            throw new BuildException(
                $"Exit code of {fileName} was {process.ExitCode}");
        }
    }

    public static string FetchChangelogVersion(
        string changelogFile)
    {
        var match = Regex.Match(
            File.ReadAllText(changelogFile),
            @"##\s+(\d+\.\d+\.\d+)");

        if (match.Groups.Count < 2)
        {
            throw new BuildException(
                $"Could not fetch version from {changelogFile}");
        }

        return match.Groups[1].Value;
    }
}
