namespace Chunkyard.Infrastructure;

/// <summary>
/// Writes a file atomically. Data is written to a temporary file next to the
/// target and is moved into place. Disposing the instance without committing
/// discards the data.
/// </summary>
public sealed class AtomicFile : IDisposable
{
    private readonly string _file;
    private readonly bool _overwrite;
    private readonly string _tempFile;

    public AtomicFile(string file, bool overwrite)
    {
        _file = file;
        _overwrite = overwrite;
        _tempFile = $"{file}.{Path.GetRandomFileName()}.tmp";

        PathUtils.EnsureParent(file);

        Stream = new FileStream(
            _tempFile,
            FileMode.CreateNew,
            FileAccess.Write);
    }

    public FileStream Stream { get; }

    public void Commit()
    {
        Stream.Dispose();
        File.Move(_tempFile, _file, _overwrite);
    }

    public void Dispose()
    {
        Stream.Dispose();
        File.Delete(_tempFile);
    }
}
