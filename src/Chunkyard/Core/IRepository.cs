namespace Chunkyard.Core;

/// <summary>
/// Defines a basic contract to store and retrieve bytes. Stored data can be
/// referenced using a key. An IRepository can handle parallel operations.
/// </summary>
public interface IRepository<T>
{
    void StoreValue(T key, ReadOnlySpan<byte> value);

    void StoreValueIfNotExists(T key, ReadOnlySpan<byte> value);

    byte[] RetrieveValue(T key);

    bool ValueExists(T key);

    IReadOnlyCollection<T> ListKeys();

    void RemoveValue(T key);
}
