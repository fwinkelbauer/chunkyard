using System.Security.Cryptography;

namespace Chunkyard
{
    internal class ChunkyardConfig
    {
        public ChunkyardConfig(string logName, HashAlgorithmName hashAlgorithmName, int minChunkSizeInByte, int avgChunkSizeInByte, int maxChunkSizeInByte, bool useCache)
        {
            LogName = logName;
            HashAlgorithmName = hashAlgorithmName;
            MinChunkSizeInByte = minChunkSizeInByte;
            AvgChunkSizeInByte = avgChunkSizeInByte;
            MaxChunkSizeInByte = maxChunkSizeInByte;
            UseCache = useCache;
        }

        public string LogName { get; }

        public HashAlgorithmName HashAlgorithmName { get; }

        public int MinChunkSizeInByte { get; }

        public int AvgChunkSizeInByte { get; }

        public int MaxChunkSizeInByte { get; }

        public bool UseCache { get; }
    }
}
