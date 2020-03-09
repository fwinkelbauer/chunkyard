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
                var decryptedData = AesGcmCrypto.Decrypt(
                    Repository.RetrieveContentChecked(chunk.ContentUri),
                    chunk.Tag,
                    key,
                    contentReference.Nonce);

                stream.Write(LzmaCompression.Decompress(decryptedData));
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
                var compressedData = LzmaCompression.Compress(chunkedData);
                var (encryptedData, tag) = AesGcmCrypto.Encrypt(compressedData, key, nonce);

                yield return new Chunk(
                    Repository.StoreContent(config.HashAlgorithmName, encryptedData),
                    tag);
            }
        }
    }
}
