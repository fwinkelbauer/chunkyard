using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
namespace Chunkyard
{
    internal static class FileFetcher
    {
        public static IEnumerable<string> FindRelative(string rootDirectory, IEnumerable<string> filters)
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
                    var files = ListFiles(rootDirectory, value);
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

            var configFiles = new[]
            {
                ToRelative(rootDirectory, Command.FiltersFileName),
                ToRelative(rootDirectory, Command.ConfigFileName)
            };

            // Make sure that the our config files are saved
            foreach (var configFile in configFiles)
            {
                if (File.Exists(configFile) &&
                    !selectedFiles.Contains(configFile))
                {
                    selectedFiles.Add(configFile);
                }
            }

            return selectedFiles;
        }

        private static List<string> ListFiles(string rootDirectory, string subDirectory)
        {
            var allFiles = Directory.EnumerateFiles(subDirectory, "*", SearchOption.AllDirectories)
                .Select(f => ToRelative(rootDirectory, f))
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

        private static string ToRelative(string rootDirectory, string file)
        {
            return Path.GetRelativePath(rootDirectory, Path.GetFullPath(file));
        }
    }
}
