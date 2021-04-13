using System;
using System.Collections.Generic;
using System.Linq;

namespace Chunkyard.Core
{
    /// <summary>
    /// Describes the difference between two instances of <see
    /// cref="Snapshot"/>.
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
    }
}
