namespace Chunkyard.Tests.Infrastructure;

internal sealed class DisposableDirectory : IDisposable
{
    public DisposableDirectory(string? directory = null)
    {
        Name = Path.Combine(
            directory ?? Path.GetTempPath(),
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
