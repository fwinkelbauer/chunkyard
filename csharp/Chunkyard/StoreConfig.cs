using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Chunkyard
{
    internal class StoreConfig
    {
        public StoreConfig(
            string contentName,
            HashAlgorithmName hashAlgorithmName,
            Func<string, byte[]> nonceGenerator,
            byte[] key,
            int minChunkSizeInByte,
            int avgChunkSizeInByte,
            int maxChunkSizeInByte)
        {
            ContentName = contentName;
            HashAlgorithmName = hashAlgorithmName;
            NonceGenerator = nonceGenerator;
            Key = key;
            MinChunkSizeInByte = minChunkSizeInByte;
            AvgChunkSizeInByte = avgChunkSizeInByte;
            MaxChunkSizeInByte = maxChunkSizeInByte;
        }

        public string ContentName { get; }

        public HashAlgorithmName HashAlgorithmName { get; }

        public Func<string, byte[]> NonceGenerator { get; }

        public byte[] Key { get; }

        public int MinChunkSizeInByte { get; }

        public int AvgChunkSizeInByte { get; }

        public int MaxChunkSizeInByte { get; }
    }
}
