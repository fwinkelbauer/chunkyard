namespace Chunkyard.Command;

/// <summary>
/// An <see cref="ICommand"/> that removes unreferenced chunks.
/// </summary>
public sealed record GarbageCollectCommand(
    SnapshotStore SnapshotStore) : ICommand
{
    public int Run()
    {
        SnapshotStore.GarbageCollect();

        return 0;
    }

    public static GarbageCollectCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore))
        {
            return new GarbageCollectCommand(snapshotStore);
        }
        else
        {
            return null;
        }
    }
}
