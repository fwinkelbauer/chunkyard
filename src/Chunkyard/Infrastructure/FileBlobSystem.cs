namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IBlobSystem"/> using the file system.
/// </summary>
public sealed class FileBlobSystem : IBlobSystem
{
    private readonly string[] _directories;
    private readonly string _common;

    public FileBlobSystem(params string[] directories)
    {
        _directories = directories
            .Select(Path.GetFullPath)
            .ToArray();

        _common = PathUtils.GetCommon(_directories);
    }

    public Blob[] ListBlobs()
    {
        return _directories
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
            ? new Blob(blobName, File.GetLastWriteTimeUtc(file))
            : null;
    }

    public Stream OpenWrite(Blob blob)
    {
        var file = ToFile(blob.Name);

        PathUtils.EnsureParent(file);

        return new WriteStream(file, blob);
    }

    private Blob ToBlob(string file)
    {
        var blobName = string.IsNullOrEmpty(_common)
            ? file
            : Path.GetRelativePath(_common, file);

        // Using a blob name with backslashes will not create subdirectories
        // when restoring a file on Linux.
        //
        // Also, we don't want to include any ":" so that Windows drive letters
        // can be turned into valid paths.
        blobName = blobName
            .Replace('\\', '/')
            .Replace(":", "");

        return new Blob(
            blobName,
            File.GetLastWriteTimeUtc(file));
    }

    private string ToFile(string blobName)
    {
        return Path.Combine(_common, blobName);
    }

    private sealed class WriteStream : FileStream
    {
        private readonly Blob _blob;

        public WriteStream(string file, Blob blob)
            : base(file, FileMode.Create, FileAccess.Write)
        {
            _blob = blob;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            File.SetLastWriteTimeUtc(
                Name,
                _blob.LastWriteTimeUtc);
        }
    }
}
