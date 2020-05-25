using System;
using System.Collections.Generic;

namespace Chunkyard
{
    /// <summary>
    /// A reference which describes how to retrieve encrypted and chunked data
    /// from a <see cref="ContentStore"/>.
    /// </summary>
    public class ContentReference
    {
        public ContentReference(
            string name,
            byte[] nonce,
            IEnumerable<ChunkReference> chunks)
        {
            Name = name;
            Nonce = nonce;
            Chunks = new List<ChunkReference>(chunks);
        }

        public string Name { get; }

        public byte[] Nonce { get; }

        public IEnumerable<ChunkReference> Chunks { get; }

        public override bool Equals(object? obj)
        {
            return obj is ContentReference reference
                && Name == reference.Name
                && EqualityComparer<byte[]>.Default.Equals(
                    Nonce,
                    reference.Nonce)
                && EqualityComparer<IEnumerable<ChunkReference>>.Default.Equals(
                    Chunks,
                    reference.Chunks);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Nonce, Chunks);
        }
    }
}
