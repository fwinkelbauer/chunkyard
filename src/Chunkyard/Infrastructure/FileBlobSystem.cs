namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IBlobSystem"/> using the file system.
/// </summary>
public class FileBlobSystem : IBlobSystem
{
    private readonly string[] _files;
    private readonly string _parent;

    public FileBlobSystem(
        IEnumerable<string> files)
    {
        _files = files.Select(Path.GetFullPath)
            .ToArray();

        _parent = FindCommonParent(_files);
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
        var foundFiles = _files
            .SelectMany(f => Find(f, excludeFuzzy))
            .Distinct();

        return foundFiles
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

        var file = ToFile(blob.Name);

        DirectoryUtils.CreateParent(file);

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
        return DirectoryUtils.CombinePathSafe(_parent, blobName);
    }

    private static IEnumerable<string> Find(
        string file,
        Fuzzy excludeFuzzy)
    {
        IEnumerable<string>? files = null;

        if (Directory.Exists(file))
        {
            files = Directory.GetFiles(
                file,
                "*",
                SearchOption.AllDirectories);
        }
        else if (File.Exists(file))
        {
            files = new[] { file };
        }
        else
        {
            throw new FileNotFoundException("Could not find file", file);
        }

        return files.Where(f => !excludeFuzzy.IsExcludingMatch(f));
    }

    private static string FindCommonParent(string[] files)
    {
        if (files.Length == 0)
        {
            throw new ChunkyardException(
                "Cannot operate on empty file list");
        }
        else if (files.Length == 1)
        {
            return File.Exists(files[0])
                ? DirectoryUtils.GetParent(files[0])
                : files[0];
        }

        var parent = "";
        var fileSegments = files
            .OrderBy(file => file)
            .Last()
            .Split(
                Path.DirectorySeparatorChar,
                StringSplitOptions.RemoveEmptyEntries)
            .ToArray();

        foreach (var fileSegment in fileSegments)
        {
            var newParent = parent + Path.DirectorySeparatorChar + fileSegment;

            if (parent.Length == 0
                && files.All(file => file.StartsWith(fileSegment)))
            {
                parent = fileSegment;
            }
            else if (files.All(file => file.StartsWith(newParent)))
            {
                parent = newParent;
            }
            else
            {
                break;
            }
        }

        return parent;
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
