namespace Chunkyard
{
    internal class RetrieveConfig
    {
        public RetrieveConfig(byte[] key)
        {
            Key = key;
        }

        public byte[] Key { get; }
    }
}
