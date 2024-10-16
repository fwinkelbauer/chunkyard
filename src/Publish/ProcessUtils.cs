namespace Publish;

/// <summary>
/// A set of process related utility methods.
/// </summary>
public static class ProcessUtils
{
    public static void Run(string fileName, string[] arguments)
    {
        using var process = Process.Start(
            fileName,
            string.Join(' ', arguments))!;

        WaitForSuccess(process);
    }

    public static string[] Capture(string fileName, string[] arguments)
    {
        using var process = Process.Start(
            new ProcessStartInfo(fileName, string.Join(' ', arguments))
            {
                RedirectStandardOutput = true
            })!;

        var lines = Capture(process.StandardOutput);

        WaitForSuccess(process);

        return lines;
    }

    private static string[] Capture(StreamReader reader)
    {
        var lines = new List<string>();
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            lines.Add(line);
        }

        return lines.ToArray();
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
