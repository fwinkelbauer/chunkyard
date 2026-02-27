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
        var blobs = SnapshotStore.GetSnapshot(SnapshotId)
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
            & consumer.TrySnapshot(out var snapshot)
            & consumer.TryInclude(out var include))
        {
            return new ShowCommand(snapshotStore, snapshot, include);
        }
        else
        {
            return null;
        }
    }
}
