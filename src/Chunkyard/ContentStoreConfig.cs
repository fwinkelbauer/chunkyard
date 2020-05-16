using System.Security.Cryptography;

namespace Chunkyard
{
    internal class ContentStoreConfig
    {
        public ContentStoreConfig(
            HashAlgorithmName hashAlgorithmName,
            int minChunkSizeInByte,
            int avgChunkSizeInByte,
            int maxChunkSizeInByte)
        {
            HashAlgorithmName = hashAlgorithmName;
            MinChunkSizeInByte = minChunkSizeInByte;
            AvgChunkSizeInByte = avgChunkSizeInByte;
            MaxChunkSizeInByte = maxChunkSizeInByte;
        }

        public HashAlgorithmName HashAlgorithmName { get; }

        public int MinChunkSizeInByte { get; }

        public int AvgChunkSizeInByte { get; }

        public int MaxChunkSizeInByte { get; }
    }
}
