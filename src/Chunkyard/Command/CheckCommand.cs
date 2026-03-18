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
        var snapshotId = SnapshotId >= 0
            ? SnapshotId
            : SnapshotStore.ListSnapshotIds()[^1];

        if (!SnapshotStore.CheckSnapshot(snapshotId, Include))
        {
            Console.Error.WriteLine(
                "Snapshot contains invalid or missing chunks");

            return 1;
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
