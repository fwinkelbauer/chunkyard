namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IBlobSystem"/> using the file system.
/// </summary>
public sealed class FileBlobSystem : IBlobSystem
{
    private readonly Lazy<string[]> _directories;
    private readonly Lazy<string> _common;

    public FileBlobSystem(params string[] directories)
    {
        _directories = new(
            () => directories.Select(Path.GetFullPath).ToArray());

        _common = new(() => PathUtils.GetCommon(_directories.Value));
    }

    public Blob[] ListBlobs()
    {
        return _directories.Value
            .SelectMany(d => Directory.GetFiles(d, "*", SearchOption.AllDirectories))
            .Distinct()
            .OrderBy(f => f)
            .Select(ToBlob)
            .ToArray();
    }

    public Stream OpenRead(string blobName)
    {
        return File.OpenRead(
            ToFile(blobName));
    }

    public Blob? GetBlob(string blobName)
    {
        var file = ToFile(blobName);

        return File.Exists(file)
            ? ToBlob(file)
            : null;
    }

    public void Write(Blob blob, Action<Stream> write)
    {
        var file = ToFile(blob.Name);

        PathUtils.EnsureParent(file);

        using (var stream = new FileStream(
            file,
            FileMode.Create,
            FileAccess.Write))
        {
            write(stream);
        }

        File.SetLastWriteTimeUtc(
            file,
            blob.LastWriteTimeUtc);
    }

    private Blob ToBlob(string file)
    {
        var blobName = string.IsNullOrEmpty(_common.Value)
            ? file
            : Path.GetRelativePath(_common.Value, file);

        return new Blob(
            UnifyForAllOperatingSystems(blobName),
            UnifyForAllFileSystems(File.GetLastWriteTimeUtc(file)));
    }

    private static string UnifyForAllOperatingSystems(string blobName)
    {
        return blobName.Replace('\\', '/').Replace(":", "");
    }

    private static DateTime UnifyForAllFileSystems(DateTime d)
    {
        return new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second);
    }

    private string ToFile(string blobName)
    {
        return Path.Combine(_common.Value, blobName);
    }
}
