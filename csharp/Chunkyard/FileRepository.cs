using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Chunkyard
{
    public class FileRepository : IRepository
    {
        private readonly string _contentDirectory;
        private readonly string _refLogDirectory;

        public FileRepository(string directory)
        {
            _contentDirectory = Path.Combine(directory, "content");
            _refLogDirectory = Path.Combine(directory, "reflog");
        }

        public Uri StoreContent(HashAlgorithmName algorithm, byte[] value)
        {
            var contentUri = Hash.ComputeContentUri(algorithm, value);

            if (!ContentExists(contentUri))
            {
                using var fileStream = new FileStream(
                    ToFilePath(contentUri),
                    FileMode.CreateNew);

                fileStream.Write(value);
            }

            return contentUri;
        }

        public byte[] RetrieveContent(Uri contentUri)
        {
            return File.ReadAllBytes(
                ToFilePath(contentUri));
        }

        public void Remove(Uri contentUri)
        {
            File.Delete(
                ToFilePath(contentUri));
        }

        public bool ContentExists(Uri contentUri)
        {
            return File.Exists(
                ToFilePath(contentUri));
        }

        public int AppendToLog<T>(T contentRef, string logName, int currentLogPosition) where T : IContentRef
        {
            var newLogPosition = currentLogPosition + 1;

            using var fileStream = new FileStream(
                ToFilePath(logName, newLogPosition),
                FileMode.CreateNew);

            fileStream.Write(
                Encoding.UTF8.GetBytes(
                    DataConvert.SerializeObject(contentRef)));

            return newLogPosition;
        }

        public T RetrieveFromLog<T>(string logName, int logPosition) where T : IContentRef
        {
            return DataConvert.DeserializeObject<T>(
                File.ReadAllText(
                    ToFilePath(logName, logPosition)));
        }

        public bool TryFetchLogPosition(string logName, out int currentLogPosition)
        {
            var logPositions = ListLog(logName).ToList();

            if (logPositions.Count == 0)
            {
                currentLogPosition = -1;
                return false;
            }

            currentLogPosition = logPositions[logPositions.Count - 1];

            return true;
        }

        public IEnumerable<int> ListLog(string logName)
        {
            var refDirectory = ToDirectoryPath(logName);
            var files = Directory.GetFiles(
                refDirectory,
                "*.json");

            foreach (var file in files)
            {
                yield return Convert.ToInt32(
                    Path.GetFileNameWithoutExtension(file));
            }
        }

        private string ToDirectoryPath(string logName)
        {
            var refDirectory = Path.Combine(_refLogDirectory, logName);
            Directory.CreateDirectory(refDirectory);

            return refDirectory;
        }

        private string ToFilePath(string logName, int logPosition)
        {
            return Path.Combine(
                ToDirectoryPath(logName),
                $"{logPosition}.json");
        }

        private string ToFilePath(Uri contentUri)
        {
            var directoryPath = Path.Combine(
                _contentDirectory,
                Hash.AlgorithmFromContentUri(contentUri).Name);

            Directory.CreateDirectory(directoryPath);

            return Path.Combine(
                directoryPath,
                Hash.HashFromContentUri(contentUri));
        }
    }
}
