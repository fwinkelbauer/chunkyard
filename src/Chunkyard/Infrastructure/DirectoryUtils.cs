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
            throw new ChunkyardException(
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
            throw new ChunkyardException(
                "Invalid directory traversal");
        }

        return absolutePath;
    }
}
