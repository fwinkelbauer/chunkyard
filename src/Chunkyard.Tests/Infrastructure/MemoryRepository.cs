namespace Chunkyard.Tests.Infrastructure;

internal sealed class MemoryRepository : IRepository
{
    public MemoryRepository()
    {
        References = new MemoryRepository<int>();
        Chunks = new MemoryRepository<string>();
    }

    public IRepository<int> References { get; }

    public IRepository<string> Chunks { get; }
}

internal sealed class MemoryRepository<T> : IRepository<T>
    where T : notnull
{
    private readonly object _lock;
    private readonly Dictionary<T, byte[]> _valuesPerKey;

    public MemoryRepository()
    {
        _lock = new object();
        _valuesPerKey = new Dictionary<T, byte[]>();
    }

    public void Store(T key, ReadOnlySpan<byte> value)
    {
        lock (_lock)
        {
            _valuesPerKey.Add(key, value.ToArray());
        }
    }

    public void StoreIfNotExists(T key, ReadOnlySpan<byte> value)
    {
        lock (_lock)
        {
            if (!_valuesPerKey.ContainsKey(key))
            {
                _valuesPerKey.Add(key, value.ToArray());
            }
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

    public IReadOnlyCollection<T> List()
    {
        lock (_lock)
        {
            return _valuesPerKey.Keys.ToArray();
        }
    }

    public bool TryLast(out T? key)
    {
        var keys = List();

        if (keys.Any())
        {
            key = keys.Max();
            return true;
        }
        else
        {
            key = default(T);
            return false;
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
