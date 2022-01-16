namespace Chunkyard.Build;

internal static class BuildUtils
{
    public static string ExecQuery(
        string fileName,
        string[] arguments,
        int[] validExitCodes)
    {
        var builder = new StringBuilder();

        Exec(
            fileName,
            arguments,
            validExitCodes,
            line => builder.AppendLine(line));

        return builder.ToString().Trim();
    }

    public static void Exec(
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

    public static void CreateCleanDirectory(string directory)
    {
        var dirInfo = new DirectoryInfo(directory);

        dirInfo.Create();

        foreach (var fileInfo in dirInfo.GetFiles())
        {
            fileInfo.Delete();
        }

        foreach (var subDirInfo in dirInfo.GetDirectories())
        {
            subDirInfo.Delete(true);
        }
    }
}
