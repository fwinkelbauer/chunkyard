namespace Chunkyard.Core;

public sealed class SimpleOrderedRepository<T> : IOrderedRepository<T>
    where T : struct
{
    private readonly IRepository<T> _repository;

    public SimpleOrderedRepository(IRepository<T> repository)
    {
        _repository = repository;
    }

    public T? RetrieveLastKey()
    {
        return _repository.ListKeys()
            .Select(key => key as T?)
            .Max();
    }

    public void StoreValue(T key, ReadOnlySpan<byte> value)
    {
        _repository.StoreValue(key, value);
    }

    public void StoreValueIfNotExists(T key, ReadOnlySpan<byte> value)
    {
        _repository.StoreValueIfNotExists(key, value);
    }

    public byte[] RetrieveValue(T key)
    {
        return _repository.RetrieveValue(key);
    }

    public bool ValueExists(T key)
    {
        return _repository.ValueExists(key);
    }

    public IReadOnlyCollection<T> ListKeys()
    {
        return _repository.ListKeys()
            .OrderBy(key => key)
            .ToArray();
    }

    public void RemoveValue(T key)
    {
        _repository.RemoveValue(key);
    }
}
