using System;
using System.Collections.Generic;

namespace Chunkyard.Tests
{
    public class MemoryRepository : IRepository
    {
        private readonly Dictionary<Uri, byte[]> _valuesByUri;
        private readonly List<byte[]> _valuesLog;

        public MemoryRepository()
        {
            _valuesByUri = new Dictionary<Uri, byte[]>();
            _valuesLog = new List<byte[]>();
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

        public IEnumerable<Uri> ListUris()
        {
            return _valuesByUri.Keys;
        }

        public void RemoveUri(Uri contentUri)
        {
            _valuesByUri.Remove(contentUri);
        }

        public int AppendToLog(byte[] value, int newLogPosition)
        {
            _valuesLog.Add(value);

            return _valuesLog.Count;
        }

        public byte[] RetrieveFromLog(int logPosition)
        {
            return _valuesLog[logPosition - 1];
        }

        public void RemoveFromLog(int logPosition)
        {
            _valuesLog.RemoveAt(logPosition);
        }

        public IEnumerable<int> ListLogPositions()
        {
            for (int i = 1; i <= _valuesLog.Count; i++)
            {
                yield return i;
            }
        }
    }
}
