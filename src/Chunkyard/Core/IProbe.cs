namespace Chunkyard.Core;

/// <summary>
/// Defines a set of events which are created while using an instance of a
/// <see cref="SnapshotStore"/>.
/// </summary>
public interface IProbe
{
    void StoredBlob(Blob blob);

    void RestoredBlob(Blob blob);

    void CheckedBlob(Blob blob, bool ok);
}
