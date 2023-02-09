namespace Chunkyard.Tests.Infrastructure;

internal static class MemoryRepository
{
    public static Repository Create()
    {
        return new Repository(
            new MemoryRepository<int>(),
            new MemoryRepository<string>());
    }
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

    public void Remove(T key)
    {
        lock (_lock)
        {
            _valuesPerKey.Remove(key);
        }
    }
}
