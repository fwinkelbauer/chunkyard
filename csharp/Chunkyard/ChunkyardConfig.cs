using System.Security.Cryptography;

namespace Chunkyard
{
    internal class ChunkyardConfig
    {
        public ChunkyardConfig(HashAlgorithmName hashAlgorithmName, int minChunkSizeInByte, int avgChunkSizeInByte, int maxChunkSizeInByte, bool useCache)
        {
            HashAlgorithmName = hashAlgorithmName;
            MinChunkSizeInByte = minChunkSizeInByte;
            AvgChunkSizeInByte = avgChunkSizeInByte;
            MaxChunkSizeInByte = maxChunkSizeInByte;
            UseCache = useCache;
        }

        public HashAlgorithmName HashAlgorithmName { get; }

        public int MinChunkSizeInByte { get; }

        public int AvgChunkSizeInByte { get; }

        public int MaxChunkSizeInByte { get; }

        public bool UseCache { get; }
    }
}
