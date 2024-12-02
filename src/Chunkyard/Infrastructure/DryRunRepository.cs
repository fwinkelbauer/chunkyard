namespace Chunkyard.Infrastructure;

/// <summary>
/// A <see cref="IRepository"/> decorator that does not persist data changes.
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

    public static IRepository Create(IRepository repository, bool dryRun)
    {
        return dryRun
            ? new DryRunRepository(repository)
            : repository;
    }
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
