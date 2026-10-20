namespace Chunkyard.Infrastructure;

/// <summary>
/// A set of path related utility methods.
/// </summary>
public static class PathUtils
{
    public static void EnsureParent(string path)
    {
        var parent = Path.GetDirectoryName(path);

        if (string.IsNullOrEmpty(parent)
            || parent.Equals(Path.GetPathRoot(parent)))
        {
            return;
        }

        _ = Directory.CreateDirectory(parent);
    }

    public static string GetCommon(string[] directories)
    {
        return GetCommon(directories, Path.DirectorySeparatorChar);
    }

    public static string GetCommon(string[] directories, char separatorChar)
    {
        if (directories.Length == 0)
        {
            return "";
        }
        else if (directories.Length == 1)
        {
            return directories[0];
        }

        var root = GetRoot(directories[0], separatorChar);

        if (directories.Any(d => !GetRoot(d, separatorChar).Equals(root)))
        {
            return "";
        }

        var segments = directories
            .Select(d => d.Split(separatorChar, StringSplitOptions.RemoveEmptyEntries))
            .ToArray();

        var commonSegments = segments[0]
            .TakeWhile((segment, i) => segments.All(
                s => i < s.Length && s[i].Equals(segment, StringComparison.Ordinal)))
            .ToArray();

        return root + string.Join(separatorChar, commonSegments);
    }

    private static string GetRoot(string directory, char separatorChar)
    {
        return directory[..^directory.TrimStart(separatorChar).Length];
    }
}
