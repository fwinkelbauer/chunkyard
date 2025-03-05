namespace Chunkyard.Core;

public interface ICryptoFactory
{
    Crypto Create(SnapshotReference? snapshotReference);
}
