namespace Chunkyard.Command;

/// <summary>
/// An <see cref="ICommand"/> that removes a snapshot.
/// </summary>
public sealed record RemoveCommand(
    SnapshotStore SnapshotStore,
    int SnapshotId) : ICommand
{
    public int Run()
    {
        SnapshotStore.RemoveSnapshot(SnapshotId);

        return 0;
    }

    public static RemoveCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TrySnapshot(out var snapshot))
        {
            return new RemoveCommand(snapshotStore, snapshot);
        }
        else
        {
            return null;
        }
    }
}
