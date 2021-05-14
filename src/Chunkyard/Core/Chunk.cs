using System;
using System.Linq;

namespace Chunkyard.Core
{
    /// <summary>
    /// Describes a chunk of binary data.
    /// </summary>
    public class Chunk
    {
        public Chunk(
            int index,
            byte[] value)
        {
            Index = index;
            Value = value;
        }

        public int Index { get; }

        public byte[] Value { get; }

        public override bool Equals(object? obj)
        {
            return obj is Chunk other
                && Index == other.Index
                && Value.SequenceEqual(other.Value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Index,
                Value);
        }
    }
}
