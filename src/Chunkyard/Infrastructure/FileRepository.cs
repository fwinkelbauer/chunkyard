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
            Path.GetFileNameWithoutExtension);
    }

    public IRepository<int> Snapshots { get; }

    public IRepository<string> Chunks { get; }
}

public sealed class FileRepository<T> : IRepository<T>
    where T : notnull
{
    private readonly string _directory;
    private readonly Func<T, string> _toFile;
    private readonly Func<string, T> _toKey;

    public FileRepository(
        string directory,
        Func<T, string> toFile,
        Func<string, T> toKey)
    {
        _directory = Path.GetFullPath(directory);
        _toFile = toFile;
        _toKey = toKey;
    }

    public void Store(T key, ReadOnlySpan<byte> value)
    {
        var file = ToFile(key);

        DirectoryUtils.EnsureParent(file);

        using var fileStream = new FileStream(
            file,
            FileMode.CreateNew,
            FileAccess.Write);

        fileStream.Write(value);
    }

    public byte[] Retrieve(T key)
    {
        return File.ReadAllBytes(
            ToFile(key));
    }

    public bool Exists(T key)
    {
        return File.Exists(
            ToFile(key));
    }

    public T[] List()
    {
        if (!Directory.Exists(_directory))
        {
            return Array.Empty<T>();
        }

        return Directory.GetFiles(_directory, "*", SearchOption.AllDirectories)
            .Select(ToKey)
            .ToArray();
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
            key = default;
            return false;
        }
    }

    public void Remove(T key)
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
