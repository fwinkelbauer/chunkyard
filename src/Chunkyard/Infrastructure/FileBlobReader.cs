using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chunkyard.Core;

namespace Chunkyard.Infrastructure
{
    /// <summary>
    /// An implementation of <see cref="IBlobReader"/> using the file system.
    /// </summary>
    internal class FileBlobReader : IBlobReader
    {
        private readonly string[] _files;
        private readonly Fuzzy _excludeFuzzy;
        private readonly string _parent;

        public FileBlobReader(
            IEnumerable<string> files,
            Fuzzy excludeFuzzy)
        {
            _files = files.Select(Path.GetFullPath)
                .ToArray();

            _excludeFuzzy = excludeFuzzy;
            _parent = FindCommonParent(_files);
        }

        public IReadOnlyCollection<Blob> FetchBlobs()
        {
            var foundFiles = _files
                .SelectMany(f => Find(f, _excludeFuzzy))
                .Distinct()
                .ToArray();

            var blobs = foundFiles
                .Select(file =>
                {
                    var blobName = string.IsNullOrEmpty(_parent)
                        ? file
                        : Path.GetRelativePath(_parent, file);

                    // Using a blob name with backslashes will not create
                    // sub-directories when restoring a file on Linux.
                    //
                    // Also we don't want to include any ":" so that Windows
                    // drive letters can be turned into valid paths.
                    blobName = blobName
                        .Replace('\\', '/')
                        .Replace(":", "");

                    return new Blob(
                        blobName,
                        File.GetLastWriteTimeUtc(file));
                })
                .ToArray();

            return blobs;
        }

        public Stream OpenRead(string blobName)
        {
            return File.OpenRead(
                Path.Combine(_parent, blobName));
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
                return Directory.Exists(files[0])
                    ? files[0]
                    : DirectoryUtil.GetParent(files[0]);
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
