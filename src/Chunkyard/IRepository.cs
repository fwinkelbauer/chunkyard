using System;
using System.Collections.Generic;

namespace Chunkyard
{
    public interface IRepository
    {
        public Uri RepositoryUri { get; }

        void StoreUri(Uri contentUri, byte[] value);

        byte[] RetrieveUri(Uri contentUri);

        bool UriExists(Uri contentUri);

        bool UriValid(Uri contentUri);

        IEnumerable<Uri> ListUris();

        void RemoveUri(Uri contentUri);

        int AppendToLog(byte[] value, string logName, int? currentLogPosition);

        byte[] RetrieveFromLog(string logName, int logPosition);

        int? FetchLogPosition(string logName);

        IEnumerable<int> ListLogPositions(string logName);

        IEnumerable<string> ListLogNames();
    }
}
