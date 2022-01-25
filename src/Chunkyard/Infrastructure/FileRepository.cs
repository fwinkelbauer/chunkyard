namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IRepository{T}"/> using the file system.
/// </summary>
internal class FileRepository<T> : IRepository<T>
{
    private readonly ConcurrentDictionary<string, object> _locks;
    private readonly string _directory;
    private readonly Func<T, string> _toFile;
    private readonly Func<string, T> _toKey;

    public FileRepository(
        string directory,
        Func<T, string> toFile,
        Func<string, T> toKey)
    {
        _locks = new ConcurrentDictionary<string, object>();
        _directory = Path.GetFullPath(directory);
        _toFile = toFile;
        _toKey = toKey;
    }

    public void StoreValue(T key, ReadOnlySpan<byte> value)
    {
        var file = ToFile(key);

        lock (_locks.GetOrAdd(file, _ => new object()))
        {
            if (File.Exists(file))
            {
                return;
            }

            DirectoryUtils.CreateParent(file);

            using var fileStream = new FileStream(
                file,
                FileMode.CreateNew,
                FileAccess.Write);

            fileStream.Write(value);
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

        var files = Directory.GetFiles(
            _directory,
            "*",
            SearchOption.AllDirectories);

        return files
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
        var path = Path.GetFullPath(
            Path.Combine(_directory, _toFile(key)));

        if (!path.StartsWith(_directory))
        {
            throw new ChunkyardException(
                "Invalid directory traversal");
        }

        return path;
    }

    private T ToKey(string file)
    {
        return _toKey(
            Path.GetRelativePath(_directory, file));
    }
}

public static class FileRepository
{
    public static IRepository<Uri> CreateUriRepository(string directory)
    {
        return new FileRepository<Uri>(
            directory,
            contentUri =>
            {
                var (algorithm, hash) = Id.DeconstructContentUri(
                    contentUri);

                return Path.Combine(
                    algorithm,
                    hash.Substring(0, 2),
                    hash);
            },
            file =>
            {
                return Id.ToContentUri(
                    DirectoryUtils.GetParent(
                        DirectoryUtils.GetParent(file)),
                    Path.GetFileNameWithoutExtension(file));
            });
    }

    public static IRepository<int> CreateIntRepository(string directory)
    {
        return new FileRepository<int>(
            directory,
            number => number.ToString(),
            file => Convert.ToInt32(file));
    }
}
