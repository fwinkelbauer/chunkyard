namespace Chunkyard.Infrastructure;

/// <summary>
/// A set of directory utility methods.
/// </summary>
internal static class DirectoryUtils
{
    public static string GetRootParent(string path)
    {
        var parent = Path.GetDirectoryName(path);

        return string.IsNullOrEmpty(parent)
            ? path
            : GetRootParent(parent);
    }

    public static void EnsureParent(string path)
    {
        var parent = Path.GetDirectoryName(path);

        if (string.IsNullOrEmpty(parent))
        {
            return;
        }

        Directory.CreateDirectory(parent);
    }

    public static string FindCommonParent(string[] files)
    {
        if (files.Length == 0)
        {
            throw new ArgumentException(
                "Cannot operate on empty file list",
                nameof(files));
        }
        else if (files.Length == 1)
        {
            return File.Exists(files[0])
                ? GetParent(files[0])
                : files[0];
        }

        var parent = "";
        var fileSegments = files
            .OrderBy(file => file)
            .Last()
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
            .ToArray();

        foreach (var fileSegment in fileSegments)
        {
            var newParent = parent + Path.DirectorySeparatorChar + fileSegment;

            if (parent.Length == 0
                && files.All(file => file.StartsWith(fileSegment)))
            {
                parent = fileSegment;
            }
            else if (files.All(file => file.StartsWith(newParent)))
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

    public static IEnumerable<string> FindFilesUpwards(
        string directory,
        string fileName)
    {
        var currentDirectory = directory;

        do
        {
            var file = Path.Combine(currentDirectory, fileName);

            if (File.Exists(file))
            {
                yield return file;
            }

            currentDirectory = Path.GetDirectoryName(currentDirectory);
        } while (!string.IsNullOrEmpty(currentDirectory));
    }

    private static string GetParent(string path)
    {
        var parent = Path.GetDirectoryName(path);

        if (string.IsNullOrEmpty(parent))
        {
            throw new ArgumentException(
                $"Path '{path}' does not have a parent directory",
                nameof(path));
        }

        return parent;
    }
}
