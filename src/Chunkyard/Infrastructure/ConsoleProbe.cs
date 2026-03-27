namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IProbe"/> which writes to the console.
/// </summary>
internal sealed class ConsoleProbe : IProbe
{
    public void StoredBlob(Blob blob)
    {
        Console.Error.WriteLine($"Stored blob: {blob.Name}");
    }

    public void RestoredBlob(Blob blob)
    {
        Console.Error.WriteLine($"Restored blob: {blob.Name}");
    }

    public void CheckedBlob(Blob blob, bool ok)
    {
        Console.Error.WriteLine(ok
            ? $"Ok: {blob.Name}"
            : $"Defect: {blob.Name}");
    }
}
