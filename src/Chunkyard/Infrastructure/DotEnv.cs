namespace Chunkyard.Infrastructure;

/// <summary>
/// A class to populate environment variables based on a set of .env files.
/// </summary>
public static class DotEnv
{
    public const string FileName = ".env";

    private const int SplitCount = 2;

    public static void Populate(string directory)
    {
        var lines = DirectoryUtils
            .FindFilesUpwards(directory, FileName)
            .Reverse()
            .SelectMany(File.ReadAllLines);

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
}
