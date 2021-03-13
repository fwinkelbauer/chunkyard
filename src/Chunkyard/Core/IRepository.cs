using System;

namespace Chunkyard.Core
{
    /// <summary>
    /// Defines a basic contract to store and retrieve bytes. Stored data can be
    /// referenced using an URI.
    /// </summary>
    public interface IRepository
    {
        Uri RepositoryUri { get; }

        void StoreValue(Uri contentUri, byte[] value);

        byte[] RetrieveValue(Uri contentUri);

        bool ValueExists(Uri contentUri);

        Uri[] ListUris();

        void RemoveValue(Uri contentUri);

        void AppendToLog(int newLogPosition, byte[] value);

        byte[] RetrieveFromLog(int logPosition);

        void RemoveFromLog(int logPosition);

        int[] ListLogPositions();
    }
}
