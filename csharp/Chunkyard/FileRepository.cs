﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

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

            RepositoryUri = new Uri(Path.GetFullPath(directory));
        }

        public Uri RepositoryUri { get; }

        public void StoreContent(Uri contentUri, byte[] value)
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

        public byte[] RetrieveContent(Uri contentUri)
        {
            return File.ReadAllBytes(
                ToFilePath(contentUri));
        }

        public bool ContentExists(Uri contentUri)
        {
            return File.Exists(
                ToFilePath(contentUri));
        }

        public bool ContentValid(Uri contentUri)
        {
            if (contentUri == null || !ContentExists(contentUri))
            {
                return false;
            }

            var content = RetrieveContent(contentUri);
            var computedUri = Id.ComputeContentUri(
                Id.AlgorithmFromContentUri(contentUri),
                content);

            return contentUri.Equals(computedUri);
        }

        public int AppendToLog(
            byte[] value,
            string logName,
            int? currentLogPosition)
        {
            var newLogPosition = currentLogPosition.HasValue
                ? currentLogPosition.Value + 1
                : 0;

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

        public int? FetchLogPosition(string logName)
        {
            var logPositions = ListLogPositions(logName).ToList();
            logPositions.Sort();

            if (logPositions.Count == 0)
            {
                return null;
            }

            return logPositions[logPositions.Count - 1];
        }

        public IEnumerable<int> ListLogPositions(string logName)
        {
            var refDirectory = ToDirectoryPath(logName);

            if (!Directory.Exists(refDirectory))
            {
                yield break;
            }

            var files = Directory.GetFiles(
                refDirectory,
                "*.json");

            foreach (var file in files)
            {
                yield return Convert.ToInt32(
                    Path.GetFileNameWithoutExtension(file));
            }
        }

        public IEnumerable<string> ListLogNames()
        {
            return Directory.GetDirectories(_refLogDirectory)
                .Select(d => Path.GetFileName(d));
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
