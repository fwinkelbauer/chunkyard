using System.IO;

namespace Chunkyard.Infrastructure
{
    /// <summary>
    /// A set of directory utility methods.
    /// </summary>
    public static class DirectoryUtil
    {
        public static void CreateParent(string file)
        {
            var parent = Path.GetDirectoryName(file);

            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }
        }
    }
}
