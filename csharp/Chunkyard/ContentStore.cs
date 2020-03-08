using System.Collections.Generic;
using System.IO;
using Chunkyard.Core;

namespace Chunkyard
{
    internal class ContentStore : IContentStore
    {
        public ContentStore(IRepository repository)
        {
            Repository = repository;
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

        public ContentReference StoreContent(Stream stream, string contentName, byte[] nonce, byte[] key, ChunkyardConfig config)
        {
            return new ContentReference(
                contentName,
                nonce,
                WriteStream(stream, nonce, key, config));
        }

        private IEnumerable<Chunk> WriteStream(Stream stream, byte[] nonce, byte[] key, ChunkyardConfig config)
        {
            var chunkedDataItems = FastCdc.SplitIntoChunks(
                stream,
                config.MinChunkSizeInByte,
                config.AvgChunkSizeInByte,
                config.MaxChunkSizeInByte);

            foreach (var chunkedData in chunkedDataItems)
            {
                var (encryptedData, tag) = AesGcmCrypto.Encrypt(chunkedData, key, nonce);
                var compressedData = LzmaCompression.Compress(encryptedData);

                yield return new Chunk(
                    Repository.StoreContent(config.HashAlgorithmName, compressedData),
                    tag);
            }
        }
    }
}
