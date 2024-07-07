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
        var added = new List<T>();
        var changed = new List<T>();
        var removed = new List<T>();

        foreach (var key in dict1.Keys.Union(dict2.Keys))
        {
            var exists1 = dict1.TryGetValue(key, out var value1);
            var exists2 = dict2.TryGetValue(key, out var value2);

            if (exists1 && exists2 && !value1!.Equals(value2!))
            {
                changed.Add(value2!);
            }
            else if (exists1 && !exists2)
            {
                removed.Add(value1!);
            }
            else if (!exists1 && exists2)
            {
                added.Add(value2!);
            }
        }

        return new DiffSet<T>(added, changed, removed);
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
