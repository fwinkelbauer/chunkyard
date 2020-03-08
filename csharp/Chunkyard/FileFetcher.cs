using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Chunkyard.Core;

namespace Chunkyard
{
    internal static class FileFetcher
    {
        public static IEnumerable<string> FindRelative(IEnumerable<string> filters)
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
                    throw new ChunkyardException("Invalid syntax. Use '+ <regex>' or '- <regex>'");
                }
            }

            // Make sure that the our config files are saved
            foreach (var configFile in new[] { Command.FiltersFileName, Command.ConfigFileName })
            {
                if (!selectedFiles.Contains(configFile))
                {
                    selectedFiles.Add(configFile);
                }
            }

            return selectedFiles;
        }

        private static List<string> ListFiles(string directory)
        {
            var allFiles = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(directory, f))
                .ToList();

            var internalExcludeRegex = $"\\{Command.RepositoryDirectoryName}[\\\\\\/]";
            var toDelete = new List<string>();

            foreach (var excludedFile in FindMatches(internalExcludeRegex, allFiles))
            {
                toDelete.Add(excludedFile);
            }

            foreach (var file in toDelete)
            {
                allFiles.Remove(file);
            }

            return allFiles;
        }

        private static IEnumerable<string> FindMatches(string regex, IList<string> lines)
        {
            return lines.Where(l => Regex.IsMatch(l, regex));
        }
    }
}
