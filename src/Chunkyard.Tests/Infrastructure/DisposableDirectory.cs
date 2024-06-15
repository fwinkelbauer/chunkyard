namespace Chunkyard.Tests.Infrastructure;

internal sealed class DisposableDirectory : IDisposable
{
    public DisposableDirectory()
    {
        Name = Some.Directory();
    }

    public string Name { get; }

    public void Dispose()
    {
        if (Directory.Exists(Name))
        {
            Directory.Delete(Name, true);
        }
    }
}
