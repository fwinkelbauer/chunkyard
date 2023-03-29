namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IBlobSystem"/> using the file system.
/// </summary>
public sealed class FileBlobSystem : IBlobSystem
{
    private readonly string[] _paths;
    private readonly string _parent;
    private readonly Fuzzy _fuzzy;

    public FileBlobSystem(
        IEnumerable<string> paths,
        Fuzzy fuzzy)
    {
        _paths = paths.Select(Path.GetFullPath).ToArray();

        _parent = _paths.Length == 1 && !File.Exists(_paths[0])
            ? _paths[0]
            : DirectoryUtils.GetCommonParent(
                _paths,
                Path.DirectorySeparatorChar);

        _fuzzy = fuzzy;
    }

    public bool BlobExists(string blobName)
    {
        return File.Exists(
            ToFile(blobName));
    }

    public IReadOnlyCollection<Blob> ListBlobs()
    {
        return _paths
            .SelectMany(DirectoryUtils.ListFiles)
            .Where(file => _fuzzy.IsMatch(file))
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

        DirectoryUtils.EnsureParent(file);

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
        var file = DirectoryUtils.CombinePathSafe(_parent, blobName);

        if (!_fuzzy.IsMatch(file))
        {
            throw new ArgumentException(
                $"Blob {blobName} is excluded",
                nameof(blobName));
        }

        return file;
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
