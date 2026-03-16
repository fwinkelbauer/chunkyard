namespace Chunkyard.Command;

/// <summary>
/// An <see cref="ICommand"/> that shows the content of a snapshot.
/// </summary>
public sealed record ShowCommand(
    SnapshotStore SnapshotStore,
    int SnapshotId,
    Regex Include) : ICommand
{
    public int Run()
    {
        var snapshotId = SnapshotId >= 0
            ? SnapshotId
            : SnapshotStore.ListSnapshotIds()[^1];

        var blobs = SnapshotStore.GetSnapshot(snapshotId)
            .ListBlobs(Include);

        foreach (var blob in blobs)
        {
            Console.WriteLine(blob.Name);
        }

        return 0;
    }

    public static ShowCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TrySnapshot(out var snapshotId)
            & consumer.TryInclude(out var include))
        {
            return new ShowCommand(snapshotStore, snapshotId, include);
        }
        else
        {
            return null;
        }
    }
}
