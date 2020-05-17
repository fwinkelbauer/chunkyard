using System.Collections.Generic;

namespace Chunkyard
{
    public class ContentStoreConfig
    {
        public ContentStoreConfig(byte[] key, byte[] salt, int iterations)
        {
            Key = key;
            Salt = salt;
            Iterations = iterations;
        }

        public IEnumerable<byte> Key { get; }

        public IEnumerable<byte> Salt { get; }

        public int Iterations { get; }
    }
}
