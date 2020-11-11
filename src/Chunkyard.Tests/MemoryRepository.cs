using System;
using System.Collections.Generic;
using System.Linq;

namespace Chunkyard.Tests
{
    public class MemoryRepository : IRepository
    {
        private readonly object _lock;
        private readonly Dictionary<Uri, byte[]> _valuesByUri;
        private readonly Dictionary<int, byte[]> _valuesByLog;

        public MemoryRepository(Guid? repositoryId = null)
        {
            _lock = new object();
            _valuesByUri = new Dictionary<Uri, byte[]>();
            _valuesByLog = new Dictionary<int, byte[]>();

            RepositoryUri = new Uri("in://memory");
            RepositoryId = repositoryId ?? Guid.NewGuid();
        }

        public Uri RepositoryUri { get; }

        public Guid RepositoryId { get; }

        public bool StoreValue(Uri contentUri, byte[] value)
        {
            lock (_lock)
            {
                if (_valuesByUri.ContainsKey(contentUri))
                {
                    return false;
                }

                _valuesByUri[contentUri] = value;

                return true;
            }
        }

        public byte[] RetrieveValue(Uri contentUri)
        {
            lock (_lock)
            {
                return _valuesByUri[contentUri];
            }
        }

        public bool ValueExists(Uri contentUri)
        {
            lock (_lock)
            {
                return _valuesByUri.ContainsKey(contentUri);
            }
        }

        public Uri[] ListUris()
        {
            lock (_lock)
            {
                return _valuesByUri.Keys
                    .OrderByDescending(uri => uri.AbsoluteUri)
                    .ToArray();
            }
        }

        public void RemoveValue(Uri contentUri)
        {
            lock (_lock)
            {
                _valuesByUri.Remove(contentUri);
            }
        }

        public int AppendToLog(int newLogPosition, byte[] value)
        {
            lock (_lock)
            {
                _valuesByLog.Add(newLogPosition, value);

                return newLogPosition;
            }
        }

        public byte[] RetrieveFromLog(int logPosition)
        {
            lock (_lock)
            {
                return _valuesByLog[logPosition];
            }
        }

        public void RemoveFromLog(int logPosition)
        {
            lock (_lock)
            {
                _valuesByLog.Remove(logPosition);
            }
        }

        public int[] ListLogPositions()
        {
            lock (_lock)
            {
                return _valuesByLog.Keys.ToArray();
            }
        }
    }
}
