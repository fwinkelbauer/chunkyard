namespace Chunkyard.Infrastructure;

public static class ZipRepository
{
    public static Repository Create(string zipFile)
    {
        return new Repository(
            new ZipRepository<int>(
                zipFile,
                "references",
                key => key.ToString(),
                Convert.ToInt32),
            new ZipRepository<string>(
                zipFile,
                "chunks",
                key => Path.Combine(key[..2], key),
                Path.GetFileNameWithoutExtension));
    }
}

public sealed class ZipRepository<T> : IRepository<T>
    where T : notnull
{
    private static readonly object _lock = new();

    private readonly string _zipFile;
    private readonly string _parent;
    private readonly Func<T, string> _toPath;
    private readonly Func<string, T> _toKey;

    public ZipRepository(
        string zipFile,
        string parent,
        Func<T, string> toPath,
        Func<string, T> toKey)
    {
        _zipFile = zipFile;
        _parent = parent;
        _toPath = toPath;
        _toKey = toKey;
    }

    public void StoreValue(T key, ReadOnlySpan<byte> value)
    {
        using var zip = Open();

        var path = ToPath(key);

        if (zip.GetEntry(path) != null)
        {
            throw new InvalidOperationException(
                $"Zip entry {path} already exists");
        }

        var entry = zip.CreateEntry(path);

        using var writer = entry.Open();

        writer.Write(value);
    }

    public void StoreValueIfNotExists(T key, ReadOnlySpan<byte> value)
    {
        using var zip = Open();

        var path = ToPath(key);

        if (zip.GetEntry(path) != null)
        {
            return;
        }

        var entry = zip.CreateEntry(path);

        using var writer = entry.Open();

        writer.Write(value);
    }

    public byte[] RetrieveValue(T key)
    {
        using var zip = Open();

        var entry = zip.GetEntry(ToPath(key))!;

        using var reader = entry.Open();
        using var memoryStream = new MemoryStream();

        reader.CopyTo(memoryStream);

        return memoryStream.ToArray();
    }

    public bool ValueExists(T key)
    {
        using var zip = Open();

        return zip.GetEntry(ToPath(key)) != null;
    }

    public IReadOnlyCollection<T> ListKeys()
    {
        using var zip = Open();

        return zip.Entries
            .Where(e => e.FullName.StartsWith(_parent))
            .Select(e => ToKey(e.FullName))
            .ToArray();
    }

    public void RemoveValue(T key)
    {
        using var zip = Open();

        zip.GetEntry(ToPath(key))!.Delete();
    }

    private ZipArchive Open()
    {
        DirectoryUtils.EnsureParent(_zipFile);

        return new ZipArchive(
            new FileStream(_zipFile, FileMode.OpenOrCreate),
            ZipArchiveMode.Update);
    }

    private string ToPath(T key)
    {
        return Path.Combine(_parent, _toPath(key));
    }

    private T ToKey(string path)
    {
        return _toKey(
            Path.GetRelativePath(_parent, path));
    }
}
