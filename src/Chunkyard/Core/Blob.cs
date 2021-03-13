using System;

namespace Chunkyard.Core
{
    /// <summary>
    /// Describes meta data of a binary data block.
    /// </summary>
    public class Blob
    {
        public Blob(
            string name,
            DateTime creationTimeUtc,
            DateTime lastWriteTimeUtc)
        {
            Name = name;
            CreationTimeUtc = creationTimeUtc;
            LastWriteTimeUtc = lastWriteTimeUtc;
        }

        public string Name { get; }

        public DateTime CreationTimeUtc { get; }

        public DateTime LastWriteTimeUtc { get; }

        public override bool Equals(object? obj)
        {
            return obj is Blob other
                && Name == other.Name
                && CreationTimeUtc.Equals(other.CreationTimeUtc)
                && LastWriteTimeUtc.Equals(other.LastWriteTimeUtc);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Name,
                CreationTimeUtc,
                LastWriteTimeUtc);
        }
    }
}
