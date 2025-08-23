namespace Chunkyard.Tests.Infrastructure;

internal sealed class MemoryRepository : IRepository
{
    public MemoryRepository()
    {
        Snapshots = new MemoryRepository<int>();
        Chunks = new MemoryRepository<string>();
    }

    public IRepository<int> Snapshots { get; }

    public IRepository<string> Chunks { get; }
}

internal sealed class MemoryRepository<T> : IRepository<T>
    where T : notnull
{
    private readonly Dictionary<T, byte[]> _valuesPerKey;

    public MemoryRepository()
    {
        _valuesPerKey = new();
    }

    public void Write(T key, ReadOnlySpan<byte> value)
    {
        if (_valuesPerKey.ContainsKey(key))
        {
            return;
        }

        _valuesPerKey.Add(key, value.ToArray());
    }

    public Stream OpenRead(T key)
    {
        return new MemoryStream(_valuesPerKey[key]);
    }

    public bool Exists(T key)
    {
        return _valuesPerKey.ContainsKey(key);
    }

    public T[] UnorderedList()
    {
        return _valuesPerKey.Keys
            .OrderBy(_ => RandomNumberGenerator.GetInt32(
                _valuesPerKey.Count))
            .ToArray();
    }

    public void Remove(T key)
    {
        _ = _valuesPerKey.Remove(key);
    }
}
