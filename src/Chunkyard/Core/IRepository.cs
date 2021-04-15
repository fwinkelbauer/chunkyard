namespace Chunkyard.Core
{
    /// <summary>
    /// Defines a basic contract to store and retrieve bytes. Stored data can be
    /// referenced using a key.
    /// </summary>
    public interface IRepository<T>
    {
        void StoreValue(T key, byte[] value);

        byte[] RetrieveValue(T key);

        bool ValueExists(T key);

        T[] ListKeys();

        void RemoveValue(T key);
    }
}
