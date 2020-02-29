using System.Collections.Generic;
using System.Security.Cryptography;

namespace Chunkyard
{
    public class ChunkyardConfig
    {
        public ChunkyardConfig(string logName, HashAlgorithmName hashAlgorithmName, int minChunkSizeInByte, int avgChunkSizeInByte, int maxChunkSizeInByte, byte[] salt, int iterations)
        {
            LogName = logName;
            HashAlgorithmName = hashAlgorithmName;
            MinChunkSizeInByte = minChunkSizeInByte;
            AvgChunkSizeInByte = avgChunkSizeInByte;
            MaxChunkSizeInByte = maxChunkSizeInByte;
            Salt = salt;
            Iterations = iterations;
        }

        public string LogName { get; }

        public HashAlgorithmName HashAlgorithmName { get; }

        public int MinChunkSizeInByte { get; }

        public int AvgChunkSizeInByte { get; }

        public int MaxChunkSizeInByte { get; }

        public IReadOnlyCollection<byte> Salt { get; }

        public int Iterations { get; }
    }
}
