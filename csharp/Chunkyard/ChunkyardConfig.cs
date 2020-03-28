using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Chunkyard
{
    internal class ChunkyardConfig
    {
        public ChunkyardConfig(IDictionary<string, string> lookup, HashAlgorithmName hashAlgorithmName, int minChunkSizeInByte, int avgChunkSizeInByte, int maxChunkSizeInByte)
        {
            Lookup = lookup.ToImmutableDictionary();
            HashAlgorithmName = hashAlgorithmName;
            MinChunkSizeInByte = minChunkSizeInByte;
            AvgChunkSizeInByte = avgChunkSizeInByte;
            MaxChunkSizeInByte = maxChunkSizeInByte;
        }

        public ImmutableDictionary<string, string> Lookup { get; }

        public HashAlgorithmName HashAlgorithmName { get; }

        public int MinChunkSizeInByte { get; }

        public int AvgChunkSizeInByte { get; }

        public int MaxChunkSizeInByte { get; }
    }
}
