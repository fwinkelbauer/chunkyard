using System;
using System.Collections.Generic;

namespace Chunkyard.Tests
{
    public class MemoryRepository : IRepository
    {
        private readonly Dictionary<Uri, byte[]> _valuesByUri;
        private readonly Dictionary<int, byte[]> _valuesByLog;

        public MemoryRepository()
        {
            _valuesByUri = new Dictionary<Uri, byte[]>();
            _valuesByLog = new Dictionary<int, byte[]>();
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
            _valuesByLog.Add(newLogPosition, value);

            return newLogPosition;
        }

        public byte[] RetrieveFromLog(int logPosition)
        {
            return _valuesByLog[logPosition];
        }

        public void RemoveFromLog(int logPosition)
        {
            _valuesByLog.Remove(logPosition);
        }

        public IEnumerable<int> ListLogPositions()
        {
            return _valuesByLog.Keys;
        }
    }
}
