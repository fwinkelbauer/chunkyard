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
            var allFiles = Directory.EnumerateFiles(
                directory,
                "*",
                SearchOption.AllDirectories)
                .ToList();

            // TODO
            var internalExcludeFuzzy = $"\\{Command.DefaultRepository}[\\\\\\/]";
            var toRemove = new List<string>();
            var matches = FindMatches(internalExcludeFuzzy, allFiles);

            foreach (var excludedFile in matches)
            {
                toRemove.Add(excludedFile);
            }

            foreach (var file in toRemove)
            {
                allFiles.Remove(file);
            }

            return allFiles;
        }

        private static IEnumerable<string> FindMatches(string fuzzyPattern, IEnumerable<string> lines)
        {
            var fuzzy = new Fuzzy(fuzzyPattern);

            return lines.Where(l => fuzzy.IsMatch(l));
        }
    }
}
