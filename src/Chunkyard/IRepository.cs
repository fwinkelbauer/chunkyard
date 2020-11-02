using System;
using System.Collections.Generic;

namespace Chunkyard
{
    /// <summary>
    /// Defines a basic contract to store and retrieve bytes. Stored data can be
    /// referenced using an URI. Any implementation of this interface must be
    /// able to handle calls in a multithreaded environment (e.g. parallel calls
    /// to StoreValue).
    /// </summary>
    public interface IRepository
    {
        public Uri RepositoryUri { get; }

        bool StoreValue(Uri contentUri, byte[] value);

        byte[] RetrieveValue(Uri contentUri);

        bool ValueExists(Uri contentUri);

        IEnumerable<Uri> ListUris();

        void RemoveValue(Uri contentUri);

        int AppendToLog(int newLogPosition, byte[] value);

        byte[] RetrieveFromLog(int logPosition);

        void RemoveFromLog(int logPosition);

        IEnumerable<int> ListLogPositions();
    }
}
