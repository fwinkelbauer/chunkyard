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

        public byte[] Key { get; }

        public byte[] Salt { get; }

        public int Iterations { get; }
    }
}
