namespace Chunkyard.Core;

/// <summary>
/// Create an instance of <see cref="Crypto"/> based on a
/// <see cref="SnapshotReference"/>.
/// </summary>
public interface ICryptoFactory
{
    Crypto Create(SnapshotReference? snapshotReference);
}
