namespace Chunkyard.Command;

/// <summary>
/// An <see cref="ICommand"/> that lists all snapshots.
/// </summary>
public sealed record ListCommand(
    SnapshotStore SnapshotStore) : ICommand
{
    public int Run()
    {
        foreach (var snapshotId in SnapshotStore.ListSnapshotIds())
        {
            var isoDate = SnapshotStore.GetSnapshot(snapshotId)
                .CreationTimeUtc
                .ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm:ss");

            Console.WriteLine($"Snapshot #{snapshotId}: {isoDate}");
        }

        return 0;
    }

    public static ListCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore))
        {
            return new ListCommand(snapshotStore);
        }
        else
        {
            return null;
        }
    }
}
