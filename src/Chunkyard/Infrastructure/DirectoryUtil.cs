using System.IO;

namespace Chunkyard.Infrastructure
{
    /// <summary>
    /// A set of directory utility methods.
    /// </summary>
    public static class DirectoryUtil
    {
        public static string GetParent(string file)
        {
            return Path.GetDirectoryName(file)
                ?? file;
        }

        public static void CreateParent(string file)
        {
            var parent = GetParent(file);

            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }
        }
    }
}
