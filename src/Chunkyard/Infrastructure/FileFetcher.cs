using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chunkyard.Core;

namespace Chunkyard.Infrastructure
{
    /// <summary>
    /// A class to retrieve file names based on a set of directories/files and
    /// exclude patterns.
    /// </summary>
    internal static class FileFetcher
    {
        public static (string Parent, Blob[] Blobs) FindBlobs(
            IEnumerable<string> files,
            Fuzzy excludeFuzzy)
        {
            var foundFiles = FindFiles(files, excludeFuzzy);
            var parent = FindCommonParent(foundFiles);

            var blobs = foundFiles
                .Select(file =>
                {
                    var blobName = string.IsNullOrEmpty(parent)
                        ? file
                        : Path.GetRelativePath(parent, file);

                    // Using a content name with backslashes will not create
                    // sub-directories when restoring a file on Linux.
                    //
                    // Also we don't want to include any ":" so that Windows
                    // drive letters can be turned into valid paths.
                    blobName = blobName
                        .Replace('\\', '/')
                        .Replace(":", "");

                    var path = Path.Combine(parent, blobName);

                    return new Blob(
                        blobName,
                        File.GetCreationTimeUtc(path),
                        File.GetLastWriteTimeUtc(path));
                })
                .ToArray();

            return (parent, blobs);
        }

        private static string[] FindFiles(
            IEnumerable<string> files,
            Fuzzy excludeFuzzy)
        {
            return files.Select(Path.GetFullPath)
                .SelectMany(f => Find(f, excludeFuzzy))
                .Distinct()
                .ToArray();
        }

        private static IEnumerable<string> Find(
            string file,
            Fuzzy excludeFuzzy)
        {
            IEnumerable<string>? files = null;

            if (Directory.Exists(file))
            {
                files = Directory.GetFiles(
                    file,
                    "*",
                    SearchOption.AllDirectories);
            }
            else if (File.Exists(file))
            {
                files = new[] { file };
            }
            else
            {
                throw new FileNotFoundException("Could not find file", file);
            }

            return files.Where(f => !excludeFuzzy.IsMatch(f));
        }

        // https://rosettacode.org/wiki/Find_common_directory_path#C.23
        private static string FindCommonParent(string[] files)
        {
            if (files.Length == 0)
            {
                return "";
            }
            else if (files.Length == 1)
            {
                return DirectoryUtil.GetParent(files[0]);
            }

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
