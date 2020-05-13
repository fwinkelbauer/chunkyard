using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Chunkyard
{
    internal static class FileFetcher
    {
        private static readonly string HomeDirectory =
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static IEnumerable<(string, string)> Find(
            IEnumerable<string> files,
            IEnumerable<string> excludePatterns)
        {
            foreach (var file in files)
            {
                var resolvedFile = ResolvePath(file);

                foreach (var foundFile in Find(resolvedFile, excludePatterns))
                {
                    var parent = Path.GetDirectoryName(resolvedFile)
                        ?? resolvedFile;

                    var contentName = Path.GetRelativePath(parent, foundFile);

                    yield return (foundFile, contentName);
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
            IEnumerable<string> excludePatterns)
        {
            var files = Directory.EnumerateFiles(
                file,
                "*",
                SearchOption.AllDirectories)
                .ToList();

            foreach (var excludePattern in excludePatterns)
            {
                var fuzzy = new Fuzzy(excludePattern);
                var excludedFiles = files.Where(f => fuzzy.IsMatch(f));

                foreach (var excludedFile in excludedFiles)
                {
                    files.Remove(excludedFile);
                }
            }

            return files;
        }
    }
}
