namespace Chunkyard.Core;

/// <summary>
/// A factory to create instances of <see cref="DiffSet{T}"/>.
/// </summary>
public static class DiffSet
{
    public static DiffSet<T> Create<T>(
        IEnumerable<T> first,
        IEnumerable<T> second,
        Func<T, string> toKey)
    {
        var dict1 = first.ToDictionary(toKey, f => f);
        var dict2 = second.ToDictionary(toKey, s => s);

        var added = dict2.Keys
            .Except(dict1.Keys)
            .ToHashSet();

        var changed = dict1.Keys
            .Intersect(dict2.Keys)
            .Where(key => !EqualityComparer<T>.Default.Equals(dict1[key], dict2[key]))
            .ToHashSet();

        var removed = dict1.Keys
            .Except(dict2.Keys)
            .ToHashSet();

        return new DiffSet<T>(
            Filter(dict2, added),
            Filter(dict2, changed),
            Filter(dict1, removed));
    }

    private static T[] Filter<T>(Dictionary<string, T> dict, HashSet<string> keys)
    {
        return dict
            .Where(p => keys.Contains(p.Key))
            .Select(p => p.Value)
            .ToArray();
    }
}

/// <summary>
/// Describes the difference between two instances of
/// <see cref="IEnumerable{T}"/>.
/// </summary>
public sealed class DiffSet<T>
{
    public DiffSet(
        IReadOnlyCollection<T> added,
        IReadOnlyCollection<T> changed,
        IReadOnlyCollection<T> removed)
    {
        Added = added;
        Changed = changed;
        Removed = removed;
    }

    public IReadOnlyCollection<T> Added { get; }

    public IReadOnlyCollection<T> Changed { get; }

    public IReadOnlyCollection<T> Removed { get; }

    public override bool Equals(object? obj)
    {
        return obj is DiffSet<T> other
            && Added.SequenceEqual(other.Added)
            && Changed.SequenceEqual(other.Changed)
            && Removed.SequenceEqual(other.Removed);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Added,
            Changed,
            Removed);
    }
}
