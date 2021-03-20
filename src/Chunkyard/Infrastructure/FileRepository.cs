using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chunkyard.Core;

namespace Chunkyard.Infrastructure
{
    /// <summary>
    /// An implementation of <see cref="IRepository"/> using the file system.
    /// </summary>
    internal class FileRepository : IRepository
    {
        private readonly ConcurrentDictionary<string, object> _locks;
        private readonly string _contentDirectory;
        private readonly string _refLogDirectory;

        public FileRepository(string directory)
        {
            _locks = new ConcurrentDictionary<string, object>();
            _contentDirectory = Path.Combine(directory, "content");
            _refLogDirectory = Path.Combine(directory, "reflog");
        }

        public void StoreValue(Uri contentUri, byte[] value)
        {
            var file = ToFilePath(contentUri);

            lock (_locks.GetOrAdd(file, _ => new object()))
            {
                if (File.Exists(file))
                {
                    return;
                }

                DirectoryUtil.CreateParent(file);

                using var fileStream = new FileStream(
                    file,
                    FileMode.CreateNew,
                    FileAccess.Write);

                fileStream.Write(value);
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
            var file = ToFilePath(contentUri);
            var directory = DirectoryUtil.GetParent(file);

            File.Delete(file);

            if (Directory.EnumerateFileSystemEntries(directory).Any())
            {
                return;
            }

            Directory.Delete(directory);
        }

        public void AppendToLog(int newLogPosition, byte[] value)
        {
            var file = ToFilePath(newLogPosition);

            DirectoryUtil.CreateParent(file);

            using var fileStream = new FileStream(
                file,
                FileMode.CreateNew,
                FileAccess.Write);

            fileStream.Write(value);
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
                var hashAlgorithmName = Path.GetFileName(hashDirectory);
                var contentFiles = Directory.EnumerateFiles(
                    hashDirectory,
                    "*",
                    SearchOption.AllDirectories);

                foreach (var contentFile in contentFiles)
                {
                    yield return Id.ToContentUri(
                        hashAlgorithmName,
                        Path.GetFileNameWithoutExtension(contentFile));
                }
            }
        }

        private string ToFilePath(int logPosition)
        {
            return Path.Combine(
                _refLogDirectory,
                $"{logPosition}.json");
        }

        private string ToFilePath(Uri contentUri)
        {
            var (algorithm, hash) = Id.DeconstructContentUri(contentUri);
            var directory = Path.Combine(
                _contentDirectory,
                algorithm.Name!.ToLower(),
                hash.Substring(0, 2));

            return Path.Combine(
                directory,
                hash);
        }
    }
}
