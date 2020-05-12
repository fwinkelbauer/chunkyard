using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Chunkyard
{
    internal static class FileFetcher
    {
        public static IEnumerable<string> Find(
            IEnumerable<string> files,
            IEnumerable<string> excludePatterns)
        {
            var allFiles = new List<string>();

            foreach (var file in files)
            {
                allFiles.AddRange(ListFiles(file));
            }

            foreach (var excludePattern in excludePatterns)
            {
                var excludedFiles = FindMatches(excludePattern, allFiles);

                foreach (var excludedFile in excludedFiles)
                {
                    allFiles.Remove(excludedFile);
                }
            }

            return allFiles.Distinct();
        }

        private static List<string> ListFiles(string directory)
        {
            // In the future we should support:
            // - Home directories: "~/Pictures"
            // - Rooted directories: "C:\Users" and "/home/somebody"
            //
            // We also have to find a sane implementation for combining paths
            // when restoring a snapshot:
            //
            // Path.Combine("C:\Users", "C:\Something")
            if (Path.IsPathRooted(directory))
            {
                throw new ChunkyardException(
                    $"Rooted paths are currently not supported: {directory}");
            }

            return Directory.EnumerateFiles(
                directory,
                "*",
                SearchOption.AllDirectories)
                .Select(ToRelative)
                .ToList();
        }

        private static List<string> FindMatches(
            string fuzzyPattern,
            IEnumerable<string> lines)
        {
            var fuzzy = new Fuzzy(fuzzyPattern);

            return lines
                .Where(l => fuzzy.IsMatch(l))
                .ToList();
        }

        private static string ToRelative(string file)
        {
            return Path.GetRelativePath(
                Path.GetFullPath("."),
                Path.GetFullPath(file));
        }
    }
}
