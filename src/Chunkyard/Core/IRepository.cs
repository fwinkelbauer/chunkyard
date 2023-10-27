namespace Chunkyard.Core;

/// <summary>
/// This interface describes how Chunkyard stores its data.
/// </summary>
public interface IRepository
{
    string Id { get; }

    IRepository<int> Snapshots { get; }

    IRepository<string> Chunks { get; }
}

/// <summary>
/// Defines a basic contract to store and retrieve bytes. Stored data can be
/// referenced using a key. A repository can handle parallel operations.
/// </summary>
public interface IRepository<T>
{
    void Store(T key, ReadOnlySpan<byte> value);

    byte[] Retrieve(T key);

    bool Exists(T key);

    T[] UnorderedList();

    bool TryLast(out T? key);

    void Remove(T key);
}
