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

        public bool StoreValue(Uri contentUri, byte[] value)
        {
            if (_valuesByUri.ContainsKey(contentUri))
            {
                return false;
            }

            _valuesByUri[contentUri] = value;

            return true;
        }

        public byte[] RetrieveValue(Uri contentUri)
        {
            return _valuesByUri[contentUri];
        }

        public bool ValueExists(Uri contentUri)
        {
            return _valuesByUri.ContainsKey(contentUri);
        }

        public IEnumerable<Uri> ListUris()
        {
            return _valuesByUri.Keys;
        }

        public void RemoveValue(Uri contentUri)
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
