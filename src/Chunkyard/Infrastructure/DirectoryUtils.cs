namespace Chunkyard.Infrastructure;

/// <summary>
/// A set of directory utility methods.
/// </summary>
internal static class DirectoryUtils
{
    public static void EnsureParent(string path)
    {
        var parent = Path.GetDirectoryName(path);

        if (string.IsNullOrEmpty(parent))
        {
            return;
        }

        Directory.CreateDirectory(parent);
    }

    public static string GetCommonParent(string[] paths)
    {
        if (paths.Length == 0)
        {
            throw new ArgumentException(
                "Cannot operate on empty path list",
                nameof(paths));
        }
        else if (paths.Length == 1)
        {
            return paths.First();
        }

        var parent = "";
        var segments = paths
            .OrderBy(p => p)
            .Last()
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
            .ToArray();

        foreach (var segment in segments)
        {
            var newParent = parent + Path.DirectorySeparatorChar + segment;

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

        return absolutePath;
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
