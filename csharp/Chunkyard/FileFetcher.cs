using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Chunkyard
{
    internal static class FileFetcher
    {
        public static IEnumerable<string> Find(IEnumerable<string> filters)
        {
            var allFiles = new List<string>();
            var selectedFiles = new List<string>();

            foreach (var filter in filters)
            {
                if (filter.StartsWith("#")
                    || string.IsNullOrWhiteSpace(filter))
                {
                    continue;
                }

                var split = filter.Split(' ', 2);
                var sign = split[0];
                var value = split[1];

                if (sign.Equals("&"))
                {
                    var files = ListFiles(value);
                    allFiles.AddRange(files);
                    selectedFiles.AddRange(files);
                }
                else if (sign.Equals("+"))
                {
                    foreach (var includedFile in FindMatches(value, allFiles))
                    {
                        if (!selectedFiles.Contains(includedFile))
                        {
                            selectedFiles.Add(includedFile);
                        }
                    }
                }
                else if (sign.Equals("-"))
                {
                    foreach (var excludedFile in FindMatches(value, allFiles))
                    {
                        selectedFiles.Remove(excludedFile);
                    }
                }
                else
                {
                    throw new ChunkyardException(
                        "Invalid syntax. Use '# <comment>', '& <directory name>', '+ <fuzzy pattern>' or '- <fuzzy pattern>'");
                }
            }

            return selectedFiles;
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

        private static IEnumerable<string> FindMatches(
            string fuzzyPattern,
            IEnumerable<string> lines)
        {
            var fuzzy = new Fuzzy(fuzzyPattern);

            return lines.Where(l => fuzzy.IsMatch(l));
        }

        private static string ToRelative(string file)
        {
            return Path.GetRelativePath(
                Path.GetFullPath("."),
                Path.GetFullPath(file));
        }
    }
}
