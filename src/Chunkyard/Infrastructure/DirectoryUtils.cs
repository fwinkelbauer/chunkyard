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
        Directory.CreateDirectory(
            GetParent(file));
    }

    public static string CombinePathSafe(
        string absoluteDirectory,
        string relativePath)
    {
        var absolutePath1 = Path.GetFullPath(
            Path.Combine(absoluteDirectory, relativePath));

        var absolutePath2 = Path.Combine(
            Path.GetFullPath(absoluteDirectory),
            relativePath);

        if (!absolutePath1.Equals(absolutePath2))
        {
            throw new ChunkyardException(
                "Invalid directory traversal");
        }

        return absolutePath1;
    }
}
