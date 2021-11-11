namespace Chunkyard.Core
{
    /// <summary>
    /// Describes the difference between two instances of
    /// <see cref="IReadOnlyCollection{T}"/>.
    /// </summary>
    public class DiffSet
    {
        public DiffSet(
            IReadOnlyCollection<string> added,
            IReadOnlyCollection<string> changed,
            IReadOnlyCollection<string> removed)
        {
            Added = added;
            Changed = changed;
            Removed = removed;
        }

        public IReadOnlyCollection<string> Added { get; }

        public IReadOnlyCollection<string> Changed { get; }

        public IReadOnlyCollection<string> Removed { get; }

        public override bool Equals(object? obj)
        {
            return obj is DiffSet other
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

        public static DiffSet Create<T>(
            IEnumerable<T> first,
            IEnumerable<T> second,
            Func<T, string> toKey)
            where T : notnull
        {
            var dict1 = first.ToDictionary(toKey, f => f);
            var dict2 = second.ToDictionary(toKey, s => s);

            var changed = dict1.Keys.Intersect(dict2.Keys)
                .Where(key => !EqualityComparer<T>.Default.Equals(dict1[key], dict2[key]))
                .ToArray();

            return new DiffSet(
                dict2.Keys.Except(dict1.Keys).ToArray(),
                changed,
                dict1.Keys.Except(dict2.Keys).ToArray());
        }
    }
}
