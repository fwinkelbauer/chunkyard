namespace Chunkyard.Command;

/// <summary>
/// An <see cref="ICommand"/> that copies snapshots from one repository to
/// another.
/// </summary>
public sealed record CopyCommand(
    SnapshotStore SnapshotStore,
    IRepository DestinationRepository) : ICommand
{
    public int Run()
    {
        SnapshotStore.CopyTo(DestinationRepository);

        return 0;
    }

    public static CopyCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryRepository("--destination", "The destination repository path", out var repository))
        {
            return new CopyCommand(snapshotStore, repository);
        }
        else
        {
            return null;
        }
    }
}
