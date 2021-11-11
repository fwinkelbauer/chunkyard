namespace Chunkyard.Tests.Infrastructure;

internal class DisposableDirectory : IDisposable
{
    public DisposableDirectory()
    {
        Name = Path.Combine(
            Path.GetTempPath(),
            $"chunkyard-test-{Path.GetRandomFileName()}");
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
