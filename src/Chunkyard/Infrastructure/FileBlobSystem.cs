namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IBlobSystem"/> using the file system.
/// </summary>
public class FileBlobSystem : IBlobSystem
{
    private readonly string[] _paths;
    private readonly string _parent;

    public FileBlobSystem(
        IEnumerable<string> paths)
    {
        _paths = paths.Select(Path.GetFullPath)
            .ToArray();

        _parent = DirectoryUtils.FindCommonParent(_paths);
    }

    public bool BlobExists(string blobName)
    {
        return File.Exists(
            ToFile(blobName));
    }

    public void RemoveBlob(string blobName)
    {
        File.Delete(
            ToFile(blobName));
    }

    public IReadOnlyCollection<Blob> ListBlobs(Fuzzy excludeFuzzy)
    {
        return _paths
            .SelectMany(DirectoryUtils.ListFiles)
            .Where(f => !excludeFuzzy.IsExcludingMatch(f))
            .Distinct()
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
        ArgumentNullException.ThrowIfNull(blob);

        return OpenWrite(blob, FileMode.Create);
    }

    public Stream NewWrite(Blob blob)
    {
        ArgumentNullException.ThrowIfNull(blob);

        return OpenWrite(blob, FileMode.CreateNew);
    }

    private Stream OpenWrite(Blob blob, FileMode mode)
    {
        var file = ToFile(blob.Name);

        DirectoryUtils.EnsureParent(file);

        return new WriteStream(
            file,
            mode,
            blob);
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
        return DirectoryUtils.CombinePathSafe(_parent, blobName);
    }

    private sealed class WriteStream : FileStream
    {
        private readonly Blob _blob;

        public WriteStream(string file, FileMode mode, Blob blob)
            : base(file, mode, FileAccess.Write)
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
