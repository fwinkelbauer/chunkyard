using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Chunkyard
{
    public class FileRefLog<T> : IContentRefLog<T> where T : IContentRef
    {
        private readonly string _directory;

        public FileRefLog(string directory)
        {
            _directory = directory;
        }

        public int Store(T contentRef, string logName, int currentLogPosition)
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

        public T Retrieve(string logName, int logPosition)
        {
            return DataConvert.DeserializeObject<T>(
                File.ReadAllText(
                    ToFilePath(logName, logPosition)));
        }

        public bool TryFetchLogPosition(string logName, out int currentLogPosition)
        {
            var logPositions = List(logName).ToList();

            if (logPositions.Count == 0)
            {
                currentLogPosition = -1;
                return false;
            }

            currentLogPosition = logPositions[logPositions.Count - 1];

            return true;
        }

        public IEnumerable<int> List(string logName)
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
            var refDirectory = Path.Combine(_directory, logName);
            Directory.CreateDirectory(refDirectory);

            return refDirectory;
        }

        private string ToFilePath(string logName, int logPosition)
        {
            return Path.Combine(
                ToDirectoryPath(logName),
                $"{logPosition}.json");
        }
    }
}
