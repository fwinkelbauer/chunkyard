using System;
using System.Collections.Generic;

namespace Chunkyard
{
    /// <summary>
    /// Defines a basic contract to store and retrieve bytes. Stored data can be
    /// referenced using an URI.
    /// </summary>
    public interface IRepository
    {
        public Uri RepositoryUri { get; }

        void StoreValue(Uri contentUri, byte[] value);

        byte[] RetrieveValue(Uri contentUri);

        bool ValueExists(Uri contentUri);

        IEnumerable<Uri> ListUris();

        void RemoveValue(Uri contentUri);

        int AppendToLog(byte[] value, int newLogPosition);

        byte[] RetrieveFromLog(int logPosition);

        void RemoveFromLog(int logPosition);

        IEnumerable<int> ListLogPositions();
    }
}
