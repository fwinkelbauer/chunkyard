using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Chunkyard
{
    /// <summary>
    /// A class to retrieve file names based on a set of directories/files and
    /// exclude patterns.
    /// </summary>
    internal static class FileFetcher
    {
        private static readonly string HomeDirectory =
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static string[] Find(
            IEnumerable<string> files,
            IEnumerable<string> excludePatterns)
        {
            return FindEnumerate(
                files,
                excludePatterns.ToArray())
                .ToArray();
        }

        private static IEnumerable<string> FindEnumerate(
            IEnumerable<string> files,
            string[] excludePatterns)
        {
            foreach (var file in files)
            {
                foreach (var path in Find(ResolvePath(file), excludePatterns))
                {
                    yield return path;
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

        public static IEnumerable<string> ToContentNames(
            string parent,
            IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                var contentName = string.IsNullOrEmpty(parent)
                    ? file
                    : file.Replace(parent, "");

                // Using a content name with backslashes will not create
                // sub-directories when restoring a file on Linux.
                //
                // Also we don't want to include any ":" so that cont name can
                // be turned into valid paths.
                yield return contentName
                    .Replace('\\', '/')
                    .Replace(":", "");
            }
        }

        // https://stackoverflow.com/questions/24866683/find-common-parent-path-in-list-of-files-and-directories
        public static string FindCommonParent(IList<string> files)
        {
            files.EnsureNotNull(nameof(files));

            if (files.Count == 0)
            {
                throw new ArgumentException(
                    "Cannot operate on empty list",
                    nameof(files));
            }

            var k = files[0].Length;

            for (int i = 1; i < files.Count; i++)
            {
                k = Math.Min(k, files[i].Length);

                for (int j = 0; j < k; j++)
                {
                    if (files[i][j] != files[0][j])
                    {
                        k = j;
                        break;
                    }
                }
            }

            return files[0].Substring(0, k);
        }
    }
}
