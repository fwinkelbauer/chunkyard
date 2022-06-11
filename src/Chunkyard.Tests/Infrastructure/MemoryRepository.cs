namespace Chunkyard.Tests.Infrastructure;

internal class MemoryRepository : IRepository
{
    public MemoryRepository()
    {
        Chunks = new MemoryRepository<string>();
        Log = new MemoryRepository<int>();
    }

    public IRepository<string> Chunks { get; }

    public IRepository<int> Log { get; }
}

internal class MemoryRepository<T> : IRepository<T>
    where T : notnull
{
    private readonly object _lock;
    private readonly Dictionary<T, byte[]> _valuesPerKey;

    public MemoryRepository()
    {
        _lock = new object();
        _valuesPerKey = new Dictionary<T, byte[]>();
    }

    public void StoreValue(T key, byte[] value)
    {
        lock (_lock)
        {
            _valuesPerKey.Add(key, value.ToArray());
        }
    }

    public byte[] RetrieveValue(T key)
    {
        lock (_lock)
        {
            return _valuesPerKey[key]
                .ToArray();
        }
    }

    public bool ValueExists(T key)
    {
        lock (_lock)
        {
            return _valuesPerKey.ContainsKey(key);
        }
    }

    public IReadOnlyCollection<T> ListKeys()
    {
        lock (_lock)
        {
            return _valuesPerKey.Keys
                .OrderBy(key => key)
                .ToArray();
        }
    }

    public void RemoveValue(T key)
    {
        lock (_lock)
        {
            _valuesPerKey.Remove(key);
        }
    }
}
