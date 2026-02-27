namespace Chunkyard.Command;

/// <summary>
/// An <see cref="ICommand"/> that stores a new snapshot.
/// </summary>
public sealed record StoreCommand(
    SnapshotStore SnapshotStore,
    IBlobSystem BlobSystem,
    Regex Include) : ICommand
{
    public int Run()
    {
        _ = SnapshotStore.StoreSnapshot(BlobSystem, DateTime.UtcNow, Include);

        return 0;
    }

    public static StoreCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryBlobSystem("--directory", "A list of directories to store", out var blobSystem)
            & consumer.TryInclude(out var include))
        {
            return new StoreCommand(snapshotStore, blobSystem, include);
        }
        else
        {
            return null;
        }
    }
}
