using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Chunkyard
{
    /// <summary>
    /// A class to retrieve file names based on a set of directories/fiels and
    /// exclude patterns.
    /// </summary>
    internal static class FileFetcher
    {
        private static readonly string HomeDirectory =
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static IEnumerable<(string AbsolutePath, string ContentName)> Find(
            IEnumerable<string> files,
            IEnumerable<string> excludePatterns)
        {
            var patterns = excludePatterns.ToArray();

            foreach (var file in files)
            {
                var resolvedPath = ResolvePath(file);
                var parentPath = Path.GetDirectoryName(resolvedPath)
                    ?? resolvedPath;

                foreach (var absolutePath in Find(resolvedPath, patterns))
                {
                    var partialPath = Path.GetRelativePath(
                        parentPath,
                        absolutePath);

                    // Using a content name with backslashes will not create
                    // sub-directories when restoring a file on Linux.
                    yield return (
                        absolutePath,
                        partialPath.Replace('\\', '/'));
                }
            }
        }

        private static string ResolvePath(string path)
        {
            if (path.StartsWith("~"))
            {
                path = path.Replace("~", HomeDirectory);
            }

            return Path.GetFullPath(path);
        }

        private static IEnumerable<string> Find(
            string file,
            string[] excludePatterns)
        {
            if (Directory.Exists(file))
            {
                return Filter(
                    Directory.GetFiles(
                        file,
                        "*",
                        SearchOption.AllDirectories),
                    excludePatterns);
            }
            else if (File.Exists(file))
            {
                return Filter(
                    new[] { file },
                    excludePatterns);
            }
            else
            {
                throw new FileNotFoundException("Could not find file", file);
            }
        }

        private static List<string> Filter(
            string[] files,
            string[] excludePatterns)
        {
            var filteredFiles = files.ToList();

            foreach (var excludePattern in excludePatterns)
            {
                var fuzzy = new Fuzzy(excludePattern);
                var excludedFiles = filteredFiles
                    .Where(f => fuzzy.IsMatch(f))
                    .ToArray();

                foreach (var excludedFile in excludedFiles)
                {
                    filteredFiles.Remove(excludedFile);
                }
            }

            return filteredFiles;
        }
    }
}
