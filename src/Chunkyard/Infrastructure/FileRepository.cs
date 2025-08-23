namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IRepository"/> using the file system.
/// </summary>
public sealed class FileRepository : IRepository
{
    public FileRepository(string directory)
    {
        Snapshots = new FileRepository<int>(
            Path.Combine(directory, "references"),
            key => key.ToString(),
            Convert.ToInt32);

        Chunks = new FileRepository<string>(
            Path.Combine(directory, "chunks"),
            key => Path.Combine(key[..2], key),
            Path.GetFileName);
    }

    public IRepository<int> Snapshots { get; }

    public IRepository<string> Chunks { get; }
}

public sealed class FileRepository<T> : IRepository<T>
    where T : notnull
{
    private readonly Lazy<string> _directory;
    private readonly Func<T, string> _toFile;
    private readonly Func<string, T> _toKey;

    public FileRepository(
        string directory,
        Func<T, string> toFile,
        Func<string, T> toKey)
    {
        _directory = new(() => Path.GetFullPath(directory));
        _toFile = toFile;
        _toKey = toKey;
    }

    public void Write(T key, ReadOnlySpan<byte> value)
    {
        var file = ToFile(key);

        if (File.Exists(file))
        {
            return;
        }

        PathUtils.EnsureParent(file);

        using var fileStream = new FileStream(
            file,
            FileMode.CreateNew,
            FileAccess.Write);

        fileStream.Write(value);
    }

    public Stream OpenRead(T key)
    {
        var file = ToFile(key);

        return File.OpenRead(file);
    }

    public bool Exists(T key)
    {
        return File.Exists(
            ToFile(key));
    }

    public T[] UnorderedList()
    {
        if (!Directory.Exists(_directory.Value))
        {
            return Array.Empty<T>();
        }

        return Directory.GetFiles(_directory.Value, "*", SearchOption.AllDirectories)
            .Select(ToKey)
            .ToArray();
    }

    public void Remove(T key)
    {
        File.Delete(
            ToFile(key));
    }

    private string ToFile(T key)
    {
        return Path.Combine(_directory.Value, _toFile(key));
    }

    private T ToKey(string file)
    {
        return _toKey(
            Path.GetRelativePath(_directory.Value, file));
    }
}
