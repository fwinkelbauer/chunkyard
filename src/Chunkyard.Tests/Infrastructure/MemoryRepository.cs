using System;
using System.Collections.Generic;
using System.Linq;
using Chunkyard.Core;

namespace Chunkyard.Tests.Infrastructure
{
    internal class MemoryRepository : IRepository
    {
        private readonly Dictionary<Uri, byte[]> _valuesByUri;
        private readonly Dictionary<int, byte[]> _valuesByLog;

        public MemoryRepository()
        {
            _valuesByUri = new Dictionary<Uri, byte[]>();
            _valuesByLog = new Dictionary<int, byte[]>();

            RepositoryUri = new Uri("in://memory");
        }

        public Uri RepositoryUri { get; }

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

        public Uri[] ListUris()
        {
            return _valuesByUri.Keys
                .OrderByDescending(uri => uri.AbsoluteUri)
                .ToArray();
        }

        public void RemoveValue(Uri contentUri)
        {
            _valuesByUri.Remove(contentUri);
        }

        public int AppendToLog(int newLogPosition, byte[] value)
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

        public int[] ListLogPositions()
        {
            return _valuesByLog.Keys.ToArray();
        }

        public void RemoveUris(IEnumerable<Uri> contentUris)
        {
            foreach (var contentUri in contentUris)
            {
                _valuesByUri.Remove(contentUri);
            }
        }

        public void CorruptUris(IEnumerable<Uri> contentUris)
        {
            foreach (var contentUri in contentUris)
            {
                _valuesByUri[contentUri] = new byte[] { 0xFF, 0xBA, 0xDD, 0xFF};
            }
        }
    }
}
