using System;
using System.Collections.Generic;

namespace Chunkyard.Tests
{
    public class MemoryRepository : IRepository
    {
        private readonly Dictionary<Uri, byte[]> _valuesByUri;
        private readonly Dictionary<string, List<byte[]>> _valuesByLog;

        public MemoryRepository()
        {
            _valuesByUri = new Dictionary<Uri, byte[]>();
            _valuesByLog = new Dictionary<string, List<byte[]>>();
        }

        public Uri RepositoryUri => new Uri("in://memory");

        public void StoreUri(Uri contentUri, byte[] value)
        {
            _valuesByUri[contentUri] = value;
        }

        public byte[] RetrieveUri(Uri contentUri)
        {
            return _valuesByUri[contentUri];
        }

        public bool UriExists(Uri contentUri)
        {
            return _valuesByUri.ContainsKey(contentUri);
        }

        public bool UriValid(Uri contentUri)
        {
            var algorithm = Id.AlgorithmFromContentUri(contentUri);
            var expectedHash = Id.HashFromContentUri(contentUri);

            var actualHash = Id.ComputeHash(
                algorithm,
                _valuesByUri[contentUri]);

            return expectedHash.Equals(actualHash);
        }

        public IEnumerable<Uri> ListUris()
        {
            return _valuesByUri.Keys;
        }

        public void RemoveUri(Uri contentUri)
        {
            _valuesByUri.Remove(contentUri);
        }

        public int AppendToLog(
            byte[] value,
            string logName,
            int? currentLogPosition)
        {
            if (!currentLogPosition.HasValue)
            {
                _valuesByLog[logName] = new List<byte[]>();
                _valuesByLog[logName].Add(value);

                return _valuesByLog[logName].Count;
            }

            var values = _valuesByLog[logName];

            if (currentLogPosition.Value != _valuesByLog[logName].Count)
            {
                throw new ChunkyardException(
                    "Optimistic concurrency exception");
            }

            values.Add(value);

            return _valuesByLog[logName].Count;
        }

        public byte[] RetrieveFromLog(string logName, int logPosition)
        {
            return _valuesByLog[logName][logPosition - 1];
        }

        public void RemoveFromLog(string logName, int logPosition)
        {
            _valuesByLog[logName].RemoveAt(logPosition);
        }

        public int? FetchLogPosition(string logName)
        {
            if (!_valuesByLog.ContainsKey(logName))
            {
                return null;
            }

            return _valuesByLog[logName].Count;
        }

        public IEnumerable<int> ListLogPositions(string logName)
        {
            for (int i = 1; i <= _valuesByLog[logName].Count; i++)
            {
                yield return i;
            }
        }

        public IEnumerable<string> ListLogNames()
        {
            return _valuesByLog.Keys;
        }
    }
}
