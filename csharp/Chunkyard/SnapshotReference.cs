using System.Collections.Generic;

namespace Chunkyard
{
    internal class SnapshotReference : ContentReference
    {
        public SnapshotReference(string name, byte[] nonce, IEnumerable<Chunk> chunks, byte[] salt, int iterations)
            : base(name, nonce, chunks)
        {
            Salt = salt;
            Iterations = iterations;
        }

        public byte[] Salt { get; }

        public int Iterations { get; }

        public static SnapshotReference FromContentReference(ContentReference contentReference, byte[] salt, int iterations)
        {
            return new SnapshotReference(
                contentReference.Name,
                contentReference.Nonce,
                contentReference.Chunks,
                salt,
                iterations);
        }
    }
}
