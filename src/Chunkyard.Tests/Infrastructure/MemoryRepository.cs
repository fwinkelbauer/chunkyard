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
    private readonly object _lock;
    private readonly Dictionary<T, byte[]> _valuesPerKey;

    public MemoryRepository()
    {
        _lock = new();
        _valuesPerKey = new();
    }

    public void Store(T key, ReadOnlySpan<byte> value)
    {
        lock (_lock)
        {
            _valuesPerKey.Add(key, value.ToArray());
        }
    }

    public byte[] Retrieve(T key)
    {
        lock (_lock)
        {
            return _valuesPerKey[key]
                .ToArray();
        }
    }

    public bool Exists(T key)
    {
        lock (_lock)
        {
            return _valuesPerKey.ContainsKey(key);
        }
    }

    public T[] UnorderedList()
    {
        lock (_lock)
        {
            return _valuesPerKey.Keys
                .OrderBy(_ => RandomNumberGenerator.GetInt32(
                    _valuesPerKey.Count))
                .ToArray();
        }
    }

    public void Remove(T key)
    {
        lock (_lock)
        {
            _valuesPerKey.Remove(key);
        }
    }
}
