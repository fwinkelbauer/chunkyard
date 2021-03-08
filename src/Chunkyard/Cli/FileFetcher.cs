using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chunkyard.Core;

namespace Chunkyard.Cli
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
                .Distinct()
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

        public static IEnumerable<Blob> FetchBlobs(
            string parent,
            IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                var blobName = string.IsNullOrEmpty(parent)
                    ? file
                    : Path.GetRelativePath(parent, file);

                // Using a content name with backslashes will not create
                // sub-directories when restoring a file on Linux.
                //
                // Also we don't want to include any ":" so that Windows drive
                // letters can be turned into valid paths.
                blobName = blobName
                    .Replace('\\', '/')
                    .Replace(":", "");

                var path = Path.Combine(parent, blobName);

                yield return new Blob(
                    () => File.OpenRead(path),
                    blobName,
                    File.GetCreationTimeUtc(path),
                    File.GetLastWriteTimeUtc(path));
            }
        }

        // https://rosettacode.org/wiki/Find_common_directory_path#C.23
        public static string FindCommonParent(IList<string> files)
        {
            var parent = "";
            var separatedPaths = files
                .First(str => str.Length == files.Max(st2 => st2.Length))
                .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
                .ToArray();

            foreach (var pathSegment in separatedPaths)
            {
                if (parent.Length == 0 && files.All(str => str.StartsWith(pathSegment)))
                {
                    parent = pathSegment;
                }
                else if (files.All(str => str.StartsWith(parent + Path.DirectorySeparatorChar + pathSegment)))
                {
                    parent += Path.DirectorySeparatorChar + pathSegment;
                }
                else
                {
                    break;
                }
            }

            return parent;
        }
    }
}
