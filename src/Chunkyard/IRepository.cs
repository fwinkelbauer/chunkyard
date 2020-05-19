using System;
using System.Collections.Generic;

namespace Chunkyard
{
    public interface IRepository
    {
        public Uri RepositoryUri { get; }

        void StoreContent(Uri contentUri, byte[] value);

        byte[] RetrieveContent(Uri contentUri);

        bool ContentExists(Uri contentUri);

        bool ContentValid(Uri contentUri);

        int AppendToLog(byte[] value, string logName, int? currentLogPosition);

        byte[] RetrieveFromLog(string logName, int logPosition);

        int? FetchLogPosition(string logName);

        IEnumerable<int> ListLogPositions(string logName);

        IEnumerable<string> ListLogNames();
    }
}
