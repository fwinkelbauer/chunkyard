namespace Chunkyard.Command;

/// <summary>
/// An <see cref="ICommand"/> that synchronizes repositories.
/// </summary>
public sealed record SyncCommand(
    IRepository FirstRepository,
    IRepository SecondRepository) : ICommand
{
    public int Run()
    {
        FirstRepository.Chunks.Sync(SecondRepository.Chunks);
        FirstRepository.Snapshots.Sync(SecondRepository.Snapshots);

        return 0;
    }

    public static SyncCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TryRepository("--first", "The first repository", out var firstRepository)
            & consumer.TryRepository("--second", "The second repository", out var secondRepository))
        {
            return new SyncCommand(firstRepository, secondRepository);
        }
        else
        {
            return null;
        }
    }
}
