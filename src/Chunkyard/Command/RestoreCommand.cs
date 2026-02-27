namespace Chunkyard.Command;

/// <summary>
/// An <see cref="ICommand"/> that restores a snapshot.
/// </summary>
public sealed record RestoreCommand(
    SnapshotStore SnapshotStore,
    IBlobSystem BlobSystem,
    int SnapshotId,
    Regex Include) : ICommand
{
    public int Run()
    {
        SnapshotStore.RestoreSnapshot(BlobSystem, SnapshotId, Include);

        return 0;
    }

    public static RestoreCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryBlobSystem("--directory", "The directory to restore into", out var blobSystem)
            & consumer.TrySnapshot(out var snapshot)
            & consumer.TryInclude(out var include))
        {
            return new RestoreCommand(snapshotStore, blobSystem, snapshot, include);
        }
        else
        {
            return null;
        }
    }
}
