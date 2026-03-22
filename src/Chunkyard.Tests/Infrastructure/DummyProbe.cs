namespace Chunkyard.Tests.Infrastructure;

/// <summary>
/// An implementation of <see cref="IProbe"/> which does nothing.
/// </summary>
internal sealed class DummyProbe : IProbe
{
    public void StoredBlob(Blob blob)
    {
    }

    public void RestoredBlob(Blob blob)
    {
    }

    public void ValidatedBlob(Blob blob, bool valid)
    {
    }
}
