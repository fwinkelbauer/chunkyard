namespace Chunkyard.Infrastructure;

/// <summary>
/// A set of directory utility methods.
/// </summary>
public static class DirectoryUtils
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

    public static string GetCommonParent(string[] paths, char separatorChar)
    {
        if (paths.Length == 0)
        {
            return "";
        }
        else if (paths.Length == 1)
        {
            return (Path.GetDirectoryName(paths[0]) ?? "")
                .Replace(Path.DirectorySeparatorChar, separatorChar);
        }

        var parent = "";
        var segments = paths
            .OrderBy(p => p)
            .Last()
            .Split(separatorChar, StringSplitOptions.RemoveEmptyEntries)
            .ToArray();

        foreach (var segment in segments)
        {
            var newParent = parent + separatorChar + segment;

            if (parent.Length == 0
                && paths.All(p => p.StartsWith(segment)))
            {
                parent = segment;
            }
            else if (paths.All(p => p.StartsWith(newParent)))
            {
                parent = newParent;
            }
            else
            {
                break;
            }
        }

        if (string.IsNullOrEmpty(parent)
            && paths.All(p => p.StartsWith(separatorChar)))
        {
            parent += separatorChar;
        }

        return parent;
    }

    public static string CombinePathSafe(string directory, string relativePath)
    {
        var absoluteDirectory = Path.GetFullPath(directory);
        var absolutePath = Path.GetFullPath(
            Path.Combine(absoluteDirectory, relativePath));

        if (!absolutePath.StartsWith(absoluteDirectory))
        {
            throw new ArgumentException(
                "Invalid directory traversal",
                nameof(relativePath));
        }

        return Path.Combine(directory, relativePath);
    }

    public static IReadOnlyCollection<string> ListFiles(string path)
    {
        if (Directory.Exists(path))
        {
            return Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        }
        else if (File.Exists(path))
        {
            return new[] { path };
        }
        else
        {
            return Array.Empty<string>();
        }
    }
}
