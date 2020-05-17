using System.Collections.Generic;

namespace Chunkyard
{
    internal class SnapshotReference
    {
        public SnapshotReference(
            ContentReference contentReference,
            byte[] salt,
            int iterations)
        {
            ContentReference = contentReference;
            Salt = salt;
            Iterations = iterations;
        }

        public ContentReference ContentReference { get; }

        public byte[] Salt { get; }

        public int Iterations { get; }

    }
}
