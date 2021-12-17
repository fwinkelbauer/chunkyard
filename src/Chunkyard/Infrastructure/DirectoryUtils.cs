namespace Chunkyard.Infrastructure;

/// <summary>
/// A set of directory utility methods.
/// </summary>
internal static class DirectoryUtils
{
    public static string GetParent(string file)
    {
        return Path.GetDirectoryName(file)
            ?? throw new ChunkyardException(
                $"File '{file}' does not have a parent directory");
    }

    public static void CreateParent(string file)
    {
        var parent = Path.GetDirectoryName(file);

        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }
    }
}
