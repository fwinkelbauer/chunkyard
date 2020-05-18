using System.Collections.Generic;
using System.Linq;

namespace Chunkyard
{
    public class SnapshotReference
    {
        public SnapshotReference(
            ContentReference contentReference,
            byte[] salt,
            int iterations)
        {
            ContentReference = contentReference;
            Salt = salt.ToList();
            Iterations = iterations;
        }

        public ContentReference ContentReference { get; }

        public IEnumerable<byte> Salt { get; }

        public int Iterations { get; }

    }
}
