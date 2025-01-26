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

        var common = "";
        var segments = directories
            .OrderBy(d => d)
            .Last()
            .Split(separatorChar, StringSplitOptions.RemoveEmptyEntries)
            .ToArray();

        foreach (var segment in segments)
        {
            var newCommon = common + separatorChar + segment;

            if (common.Length == 0
                && directories.All(d => d.StartsWith(segment)))
            {
                common = segment;
            }
            else if (directories.All(d => d.StartsWith(newCommon)))
            {
                common = newCommon;
            }
            else
            {
                break;
            }
        }

        if (string.IsNullOrEmpty(common)
            && directories.All(d => d.StartsWith(separatorChar)))
        {
            common += separatorChar;
        }

        return common;
    }
}
