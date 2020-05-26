using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Chunkyard
{
    /// <summary>
    /// An implementation of <see cref="IRepository"/> using the file system.
    /// </summary>
    public class FileRepository : IRepository
    {
        private readonly string _contentDirectory;
        private readonly string _refLogDirectory;

        public FileRepository(string directory)
        {
            _contentDirectory = Path.Combine(directory, "content");
            _refLogDirectory = Path.Combine(directory, "reflog");

            RepositoryUri = new Uri(Path.GetFullPath(directory));
        }

        public Uri RepositoryUri { get; }

        public void StoreUri(Uri contentUri, byte[] value)
        {
            var file = ToFilePath(contentUri);

            if (!File.Exists(file))
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(file));

                using var fileStream = new FileStream(
                    file,
                    FileMode.CreateNew,
                    FileAccess.Write);

                fileStream.Write(value);
                fileStream.Flush(true);
            }
        }

        public byte[] RetrieveUri(Uri contentUri)
        {
            return File.ReadAllBytes(
                ToFilePath(contentUri));
        }

        public bool UriExists(Uri contentUri)
        {
            return File.Exists(
                ToFilePath(contentUri));
        }

        public IEnumerable<Uri> ListUris()
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

        public void RemoveUri(Uri contentUri)
        {
            var filePath = ToFilePath(contentUri);
            var directoryPath = Path.GetDirectoryName(filePath);

            File.Delete(filePath);

            if (Directory.EnumerateFileSystemEntries(directoryPath).Any())
            {
                return;
            }

            Directory.Delete(directoryPath);
        }

        public int AppendToLog(
            byte[] value,
            string logName,
            int newLogPosition)
        {
            var file = ToFilePath(logName, newLogPosition);

            Directory.CreateDirectory(
                Path.GetDirectoryName(file));

            using var fileStream = new FileStream(
                file,
                FileMode.CreateNew,
                FileAccess.Write);

            fileStream.Write(value);
            fileStream.Flush(true);

            return newLogPosition;
        }

        public byte[] RetrieveFromLog(string logName, int logPosition)
        {
            return File.ReadAllBytes(
                ToFilePath(logName, logPosition));
        }

        public void RemoveFromLog(string logName, int logPosition)
        {
            File.Delete(
                ToFilePath(logName, logPosition));
        }

        public IEnumerable<int> ListLogPositions(string logName)
        {
            var refDirectory = ToDirectoryPath(logName);

            if (!Directory.Exists(refDirectory))
            {
                return new List<int>();
            }

            var files = Directory.GetFiles(
                refDirectory,
                "*.json");

            var logPositions = new List<int>();

            foreach (var file in files)
            {
                logPositions.Add(
                    Convert.ToInt32(
                        Path.GetFileNameWithoutExtension(file)));
            }

            logPositions.Sort();

            return logPositions;
        }

        public IEnumerable<string> ListLogNames()
        {
            return Directory.GetDirectories(_refLogDirectory)
                .Select(d => Path.GetFileName(d));
        }

        private static Uri ToContentUri(string filePath)
        {
            var hashAlgorithmName = Path.GetFileName(
                Path.GetDirectoryName(
                    Path.GetDirectoryName(filePath)
                    ?? string.Empty))
                ?? string.Empty;

            return Id.ToContentUri(
                hashAlgorithmName,
                Path.GetFileNameWithoutExtension(filePath));
        }

        private string ToDirectoryPath(string logName)
        {
            return Path.Combine(
                _refLogDirectory,
                logName);
        }

        private string ToFilePath(string logName, int logPosition)
        {
            return Path.Combine(
                ToDirectoryPath(logName),
                $"{logPosition}.json");
        }

        private string ToFilePath(Uri contentUri)
        {
            var hash = Id.HashFromContentUri(contentUri);
            var directory = Path.Combine(
                _contentDirectory,
                Id.AlgorithmFromContentUri(contentUri).Name.ToLower(),
                hash.Substring(0, 2));

            return Path.Combine(
                directory,
                hash);
        }
    }
}
