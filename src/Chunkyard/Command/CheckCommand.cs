namespace Chunkyard.Command;

/// <summary>
/// An <see cref="ICommand"/> that checks if a snapshot is valid.
/// </summary>
public sealed record CheckCommand(
    SnapshotStore SnapshotStore,
    int SnapshotId,
    Regex Include) : ICommand
{
    public int Run()
    {
        if (!SnapshotStore.CheckSnapshot(SnapshotId, Include))
        {
            throw new ChunkyardException(
                "Snapshot contains invalid or missing chunks");
        }

        return 0;
    }

    public static CheckCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TrySnapshot(out var snapshotId)
            & consumer.TryInclude(out var include))
        {
            return new CheckCommand(snapshotStore, snapshotId, include);
        }
        else
        {
            return null;
        }
    }
}
