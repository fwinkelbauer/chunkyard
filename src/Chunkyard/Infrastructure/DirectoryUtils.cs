namespace Chunkyard.Infrastructure;

/// <summary>
/// A set of directory utility methods.
/// </summary>
internal static class DirectoryUtils
{
    public static string GetParent(string file)
    {
        var parent = Path.GetDirectoryName(file);

        if (string.IsNullOrEmpty(parent))
        {
            throw new IOException(
                $"File '{file}' does not have a parent directory");
        }

        return parent;
    }

    public static void CreateParent(string file)
    {
        var parent = Path.GetDirectoryName(file);

        if (string.IsNullOrEmpty(parent))
        {
            return;
        }

        Directory.CreateDirectory(parent);
    }

    public static string CombinePathSafe(
        string absoluteDirectory,
        string relativePath)
    {
        var absolutePath = Path.GetFullPath(
            Path.Combine(absoluteDirectory, relativePath));

        if (!absolutePath.StartsWith(absoluteDirectory))
        {
            throw new IOException(
                "Invalid directory traversal");
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
            throw new FileNotFoundException("Could not find path", path);
        }
    }

    public static string FindCommonParent(string[] files)
    {
        if (files.Length == 0)
        {
            throw new IOException(
                "Cannot operate on empty file list");
        }
        else if (files.Length == 1)
        {
            return File.Exists(files[0])
                ? DirectoryUtils.GetParent(files[0])
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

    public static IEnumerable<string> FindFilesUpwards(string fileName)
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
