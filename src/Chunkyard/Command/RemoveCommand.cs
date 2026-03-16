namespace Chunkyard.Command;

/// <summary>
/// An <see cref="ICommand"/> that removes snapshots.
/// </summary>
public sealed record RemoveCommand(
    SnapshotStore SnapshotStore,
    IReadOnlyCollection<int> SnapshotIds) : ICommand
{
    public int Run()
    {
        foreach (var snapshotId in SnapshotIds)
        {
            SnapshotStore.RemoveSnapshot(snapshotId);
        }

        SnapshotStore.GarbageCollect();

        return 0;
    }

    public static RemoveCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TrySnapshots(out var snapshotIds))
        {
            return new RemoveCommand(snapshotStore, snapshotIds);
        }
        else
        {
            return null;
        }
    }
}
