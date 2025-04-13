namespace Chunkyard.Infrastructure;

/// <summary>
/// A <see cref="IRepository"/> decorator that does not store or remove data.
/// </summary>
public sealed class DryRunRepository : IRepository
{
    public DryRunRepository(IRepository repository)
    {
        Snapshots = new DryRunRepository<int>(repository.Snapshots);
        Chunks = new DryRunRepository<string>(repository.Chunks);
    }

    public IRepository<int> Snapshots { get; }

    public IRepository<string> Chunks { get; }
}

public sealed class DryRunRepository<T> : IRepository<T>
    where T : notnull
{
    private readonly IRepository<T> _repository;

    public DryRunRepository(IRepository<T> repository)
    {
        _repository = repository;
    }

    public void Store(T key, ReadOnlySpan<byte> value)
    {
    }

    public byte[] Retrieve(T key)
    {
        return _repository.Retrieve(key);
    }

    public bool Exists(T key)
    {
        return _repository.Exists(key);
    }

    public T[] UnorderedList()
    {
        return _repository.UnorderedList();
    }

    public void Remove(T key)
    {
    }
}
