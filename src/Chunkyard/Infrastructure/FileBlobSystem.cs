namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IBlobSystem"/> using the file system.
/// </summary>
public sealed class FileBlobSystem : IBlobSystem
{
    private readonly string[] _paths;
    private readonly string _parent;

    public FileBlobSystem(params string[] paths)
    {
        _paths = paths.Select(Path.GetFullPath).ToArray();

        _parent = _paths.Length == 1 && !File.Exists(_paths[0])
            ? _paths[0]
            : PathUtils.GetCommonParent(
                _paths,
                Path.DirectorySeparatorChar);
    }

    public bool BlobExists(string blobName)
    {
        return File.Exists(
            ToFile(blobName));
    }

    public Blob[] ListBlobs()
    {
        return _paths
            .SelectMany(ListFiles)
            .Distinct()
            .OrderBy(file => file)
            .Select(ToBlob)
            .ToArray();
    }

    public Stream OpenRead(string blobName)
    {
        return File.OpenRead(
            ToFile(blobName));
    }

    public Blob GetBlob(string blobName)
    {
        return new Blob(
            blobName,
            File.GetLastWriteTimeUtc(
                ToFile(blobName)));
    }

    public Stream OpenWrite(Blob blob)
    {
        var file = ToFile(blob.Name);

        PathUtils.EnsureParent(file);

        return new WriteStream(file, blob);
    }

    private Blob ToBlob(string file)
    {
        var blobName = string.IsNullOrEmpty(_parent)
            ? file
            : Path.GetRelativePath(_parent, file);

        // Using a blob name with backslashes will not create
        // sub-directories when restoring a file on Linux.
        //
        // Also we don't want to include any ":" so that Windows
        // drive letters can be turned into valid paths.
        blobName = blobName
            .Replace('\\', '/')
            .Replace(":", "");

        return new Blob(
            blobName,
            File.GetLastWriteTimeUtc(file));
    }

    private string ToFile(string blobName)
    {
        return Path.Combine(_parent, blobName);
    }

    private static string[] ListFiles(string path)
    {
        if (Directory.Exists(path))
        {
            return Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        }
        else if (File.Exists(path))
        {
            return new[] { path };
        }
        else
        {
            throw new IOException($"Path does not exist: {path}");
        }
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
