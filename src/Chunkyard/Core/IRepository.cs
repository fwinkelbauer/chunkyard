namespace Chunkyard.Core;

/// <summary>
/// Defines a basic contract to store and retrieve bytes. Stored data can be
/// referenced using a key.
/// </summary>
public interface IRepository<T>
{
    void StoreValue(T key, ReadOnlySpan<byte> value);

    byte[] RetrieveValue(T key);

    bool ValueExists(T key);

    IReadOnlyCollection<T> ListKeys();

    void RemoveValue(T key);
}
