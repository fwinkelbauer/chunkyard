using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chunkyard.Core;

namespace Chunkyard.Infrastructure
{
    /// <summary>
    /// An implementation of <see cref="IRepository"/> using the file system.
    /// </summary>
    public class FileRepository : IRepository
    {
        private readonly NamedMonitor _monitor;
        private readonly string _contentDirectory;
        private readonly string _refLogDirectory;

        public FileRepository(string directory)
        {
            _monitor = new NamedMonitor();
            _contentDirectory = Path.Combine(directory, "content");
            _refLogDirectory = Path.Combine(directory, "reflog");

            RepositoryUri = new Uri(Path.GetFullPath(directory));
        }

        public Uri RepositoryUri { get; }

        public bool StoreValue(Uri contentUri, byte[] value)
        {
            var file = ToFilePath(contentUri);

            lock (_monitor[file])
            {
                if (File.Exists(file))
                {
                    return false;
                }

                DirectoryUtil.CreateParent(file);

                using var fileStream = new FileStream(
                    file,
                    FileMode.CreateNew,
                    FileAccess.Write);

                fileStream.Write(value);

                return true;
            }
        }

        public byte[] RetrieveValue(Uri contentUri)
        {
            return File.ReadAllBytes(
                ToFilePath(contentUri));
        }

        public bool ValueExists(Uri contentUri)
        {
            return File.Exists(
                ToFilePath(contentUri));
        }

        public Uri[] ListUris()
        {
            return EnumerateUris().ToArray();
        }

        public void RemoveValue(Uri contentUri)
        {
            var filePath = ToFilePath(contentUri);
            var directoryPath = Path.GetDirectoryName(filePath);

            File.Delete(filePath);

            if (string.IsNullOrEmpty(directoryPath)
                || Directory.EnumerateFileSystemEntries(directoryPath).Any())
            {
                return;
            }

            Directory.Delete(directoryPath);
        }

        public int AppendToLog(int newLogPosition, byte[] value)
        {
            var file = ToFilePath(newLogPosition);

            DirectoryUtil.CreateParent(file);

            using var fileStream = new FileStream(
                file,
                FileMode.CreateNew,
                FileAccess.Write);

            fileStream.Write(value);

            return newLogPosition;
        }

        public byte[] RetrieveFromLog(int logPosition)
        {
            return File.ReadAllBytes(
                ToFilePath(logPosition));
        }

        public void RemoveFromLog(int logPosition)
        {
            File.Delete(
                ToFilePath(logPosition));
        }

        public int[] ListLogPositions()
        {
            if (!Directory.Exists(_refLogDirectory))
            {
                return Array.Empty<int>();
            }

            var files = Directory.GetFiles(
                _refLogDirectory,
                "*.json");

            var logPositions = files
                .Select(file => Convert.ToInt32(
                    Path.GetFileNameWithoutExtension(file)))
                .ToArray();

            Array.Sort(logPositions);

            return logPositions;
        }

        private IEnumerable<Uri> EnumerateUris()
        {
            if (!Directory.Exists(_contentDirectory))
            {
                yield break;
            }

            var hashDirectories = Directory.GetDirectories(
                _contentDirectory);

            foreach (var hashDirectory in hashDirectories)
            {
                var contentFiles = Directory.EnumerateFiles(
                    hashDirectory,
                    "*",
                    SearchOption.AllDirectories);

                foreach (var contentFile in contentFiles)
                {
                    yield return ToContentUri(contentFile);
                }
            }
        }

        private static Uri ToContentUri(string filePath)
        {
            var hashAlgorithmName = Path.GetFileName(
                Path.GetDirectoryName(
                    Path.GetDirectoryName(filePath)
                    ?? ""))
                ?? "";

            return Id.ToContentUri(
                hashAlgorithmName,
                Path.GetFileNameWithoutExtension(filePath));
        }

        private string ToFilePath(int logPosition)
        {
            return Path.Combine(
                _refLogDirectory,
                $"{logPosition}.json");
        }

        private string ToFilePath(Uri contentUri)
        {
            var hash = Id.HashFromContentUri(contentUri);
            var directory = Path.Combine(
                _contentDirectory,
                Id.AlgorithmFromContentUri(contentUri).Name!.ToLower(),
                hash.Substring(0, 2));

            return Path.Combine(
                directory,
                hash);
        }
    }
}
