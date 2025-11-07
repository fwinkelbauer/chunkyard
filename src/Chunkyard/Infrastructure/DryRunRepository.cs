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

/// <summary>
/// A <see cref="IRepository{T}"/> decorator that does not store or remove data.
/// </summary>
public sealed class DryRunRepository<T> : IRepository<T>
    where T : notnull
{
    private readonly IRepository<T> _repository;

    public DryRunRepository(IRepository<T> repository)
    {
        _repository = repository;
    }

    public void Write(T key, ReadOnlySpan<byte> value)
    {
    }

    public Stream OpenRead(T key)
    {
        return _repository.OpenRead(key);
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
