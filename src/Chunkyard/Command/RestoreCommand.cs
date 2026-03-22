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
        var snapshotId = SnapshotId >= 0
            ? SnapshotId
            : SnapshotStore.ListSnapshotIds()[^1];

        SnapshotStore.RestoreSnapshot(BlobSystem, snapshotId, Include);
        Console.Error.WriteLine($"Restored snapshot: #{snapshotId}");

        return 0;
    }

    public static RestoreCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryBlobSystem("--directory", "The directory to restore into", out var blobSystem)
            & consumer.TrySnapshot(out var snapshotId)
            & consumer.TryInclude(out var include))
        {
            return new RestoreCommand(snapshotStore, blobSystem, snapshotId, include);
        }
        else
        {
            return null;
        }
    }
}
