namespace Chunkyard.Command;

/// <summary>
/// An <see cref="ICommand"/> that shows the difference between two snapshots.
/// </summary>
public sealed record DiffCommand(
    SnapshotStore SnapshotStore,
    int FirstSnapshotId,
    int SecondSnapshotId,
    Regex Include) : ICommand
{
    public int Run()
    {
        var first = SnapshotStore.GetSnapshot(FirstSnapshotId)
            .ListBlobs(Include)
            .ToDictionary(b => b.Name, b => b);

        var second = SnapshotStore.GetSnapshot(SecondSnapshotId)
            .ListBlobs(Include)
            .ToDictionary(b => b.Name, b => b);

        var changes = first.Keys
            .Intersect(second.Keys)
            .Where(key => !first[key].Equals(second[key]));

        foreach (var added in second.Keys.Except(first.Keys))
        {
            Console.WriteLine($"+ {added}");
        }

        foreach (var changed in changes)
        {
            Console.WriteLine($"~ {changed}");
        }

        foreach (var removed in first.Keys.Except(second.Keys))
        {
            Console.WriteLine($"- {removed}");
        }

        return 0;
    }

    public static DiffCommand? Parse(FlagConsumer consumer)
    {
        if (consumer.TrySnapshotStore(out var snapshotStore)
            & consumer.TryInt("--first", "The first snapshot ID", out var firstSnapshotId, SnapshotStore.SecondLatestSnapshotId)
            & consumer.TryInt("--second", "The second snapshot ID", out var secondSnapshotId, SnapshotStore.LatestSnapshotId)
            & consumer.TryInclude(out var include))
        {
            return new DiffCommand(snapshotStore, firstSnapshotId, secondSnapshotId, include);
        }
        else
        {
            return null;
        }
    }
}
