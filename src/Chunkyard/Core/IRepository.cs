namespace Chunkyard.Core;

/// <summary>
/// Defines a basic contract to store and retrieve bytes. Stored data can be
/// referenced using a key. An IRepository can handle parallel operations.
/// </summary>
public interface IRepository<T>
{
    void Store(T key, ReadOnlySpan<byte> value);

    void StoreIfNotExists(T key, ReadOnlySpan<byte> value);

    byte[] Retrieve(T key);

    bool Exists(T key);

    IReadOnlyCollection<T> List();

    void Remove(T key);
}
