namespace Chunkyard.Infrastructure;

/// <summary>
/// A class to populate environment variables based on a set of .env files.
/// </summary>
internal static class DotEnv
{
    private const int SplitCount = 2;

    public static void Populate()
    {
        var files = FindFilesUpwards(".env").Reverse();
        var lines = files.SelectMany(File.ReadAllLines);

        foreach (var line in lines)
        {
            var split = line.Split(
                '=',
                SplitCount,
                StringSplitOptions.TrimEntries);

            if (split.Length == SplitCount)
            {
                Environment.SetEnvironmentVariable(split[0], split[1]);
            }
        }
    }

    private static IEnumerable<string> FindFilesUpwards(string fileName)
    {
        var directory = Directory.GetCurrentDirectory();

        do
        {
            var file = Path.Combine(directory, fileName);

            if (File.Exists(file))
            {
                yield return file;
            }

            directory = Path.GetDirectoryName(directory);
        } while (!string.IsNullOrEmpty(directory));
    }
}
