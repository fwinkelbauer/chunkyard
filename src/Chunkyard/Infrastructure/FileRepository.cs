namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IRepository"/> using the file system.
/// </summary>
public static class FileRepository
{
    public static Repository Create(string directory)
    {
        return new Repository(
            new IntFileRepository(Path.Combine(directory, "references")),
            new StringFileRepository(Path.Combine(directory, "chunks")));
    }
}

public sealed class StringFileRepository : FileRepository<string>
{
    public StringFileRepository(string directory)
        : base(
            directory,
            chunkId => Path.Combine(chunkId[..2], chunkId),
            file => Path.GetFileNameWithoutExtension(file))
    {
    }
}

public sealed class IntFileRepository : FileRepository<int>
{
    public IntFileRepository(string directory)
        : base(
            directory,
            number => number.ToString(),
            file => Convert.ToInt32(file))
    {
    }
}

public abstract class FileRepository<T> : IRepository<T>
    where T : notnull
{
    private readonly string _directory;
    private readonly Func<T, string> _toFile;
    private readonly Func<string, T> _toKey;
    private readonly ConcurrentDictionary<T, object> _locks;

    internal FileRepository(
        string directory,
        Func<T, string> toFile,
        Func<string, T> toKey)
    {
        _directory = Path.GetFullPath(directory);
        _toFile = toFile;
        _toKey = toKey;

        _locks = new ConcurrentDictionary<T, object>();
    }

    public void StoreValue(T key, ReadOnlySpan<byte> value)
    {
        var file = ToFile(key);

        DirectoryUtils.EnsureParent(file);

        using var fileStream = new FileStream(
            file,
            FileMode.CreateNew,
            FileAccess.Write);

        fileStream.Write(value);
    }

    public void StoreValueIfNotExists(T key, ReadOnlySpan<byte> value)
    {
        lock (_locks.GetOrAdd(key, _ => new object()))
        {
            if (!ValueExists(key))
            {
                StoreValue(key, value);
            }
        }
    }

    public byte[] RetrieveValue(T key)
    {
        return File.ReadAllBytes(
            ToFile(key));
    }

    public bool ValueExists(T key)
    {
        return File.Exists(
            ToFile(key));
    }

    public IReadOnlyCollection<T> ListKeys()
    {
        if (!Directory.Exists(_directory))
        {
            return Array.Empty<T>();
        }

        return Directory.GetFiles(_directory, "*", SearchOption.AllDirectories)
            .Select(ToKey)
            .ToArray();
    }

    public void RemoveValue(T key)
    {
        File.Delete(
            ToFile(key));
    }

    private string ToFile(T key)
    {
        return DirectoryUtils.CombinePathSafe(_directory, _toFile(key));
    }

    private T ToKey(string file)
    {
        return _toKey(
            Path.GetRelativePath(_directory, file));
    }
}
