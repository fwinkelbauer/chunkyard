using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Chunkyard.Core;

namespace Chunkyard
{
    internal class ContentStore : IContentStore
    {
        private readonly HashAlgorithmName _hashAlgorithmName;
        private readonly int _minChunkSizeInByte;
        private readonly int _avgChunkSizeInByte;
        private readonly int _maxChunkSizeInByte;

        public ContentStore(IRepository repository, HashAlgorithmName hashAlgorithmName, int minChunkSizeInByte, int avgChunkSizeInByte, int maxChunkSizeInByte)
        {
            Repository = repository;

            _hashAlgorithmName = hashAlgorithmName;
            _minChunkSizeInByte = minChunkSizeInByte;
            _avgChunkSizeInByte = avgChunkSizeInByte;
            _maxChunkSizeInByte = maxChunkSizeInByte;
        }

        public IRepository Repository { get; }

        public void RetrieveContent(ContentReference contentReference, Stream stream, byte[] key)
        {
            foreach (var chunk in contentReference.Chunks)
            {
                var compressedData = Repository.RetrieveContentChecked(chunk.ContentUri);
                var decompressedData = LzmaCompression.Decompress(compressedData);
                var decryptedData = AesGcmCrypto.Decrypt(
                    decompressedData,
                    chunk.Tag,
                    key,
                    contentReference.Nonce);

                stream.Write(decryptedData);
            }
        }

        public ContentReference StoreContent(Stream stream, string contentName, byte[] nonce, byte[] key)
        {
            return new ContentReference(
                contentName,
                nonce,
                WriteStream(stream, nonce, key));
        }

        private IEnumerable<Chunk> WriteStream(Stream stream, byte[] nonce, byte[] key)
        {
            var chunkedDataItems = FastCdc.SplitIntoChunks(
                stream,
                _minChunkSizeInByte,
                _avgChunkSizeInByte,
                _maxChunkSizeInByte);

            foreach (var chunkedData in chunkedDataItems)
            {
                var (encryptedData, tag) = AesGcmCrypto.Encrypt(chunkedData, key, nonce);
                var compressedData = LzmaCompression.Compress(encryptedData);

                yield return new Chunk(
                    Repository.StoreContent(_hashAlgorithmName, compressedData),
                    tag);
            }
        }
    }
}
