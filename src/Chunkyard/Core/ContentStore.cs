using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    /// <summary>
    /// An implementation of <see cref="IContentStore"/> which splits and
    /// encrypts files before storing them in an <see cref="IRepository"/>.
    /// </summary>
    public class ContentStore : IContentStore
    {
        private readonly FastCdc _fastCdc;
        private readonly HashAlgorithmName _hashAlgorithmName;

        public ContentStore(
            IRepository repository,
            FastCdc fastCdc,
            HashAlgorithmName hashAlgorithmName)
        {
            Repository = repository.EnsureNotNull(nameof(repository));
            _fastCdc = fastCdc;
            _hashAlgorithmName = hashAlgorithmName;
        }

        public IRepository Repository { get; }

        public void RetrieveContent(
            ContentReference contentReference,
            byte[] key,
            Stream outputStream)
        {
            outputStream.EnsureNotNull(nameof(outputStream));
            contentReference.EnsureNotNull(nameof(contentReference));

            foreach (var chunk in contentReference.Chunks)
            {
                // Strip away the cryptographic details which we added when
                // storing the value
                var value = Repository.RetrieveValue(chunk.ContentUri)
                    .Skip(AesGcmCrypto.NonceBytes)
                    .SkipLast(AesGcmCrypto.TagBytes)
                    .ToArray();

                var decryptedData = AesGcmCrypto.Decrypt(
                    value,
                    chunk.Tag,
                    key,
                    contentReference.Nonce);

                outputStream.Write(decryptedData);
            }
        }

        public ContentReference StoreContent(
            Stream inputStream,
            string contentName,
            byte[] key,
            byte[] nonce,
            ContentType type,
            out bool isNewContent)
        {
            return new ContentReference(
                contentName,
                nonce,
                WriteChunks(key, nonce, inputStream, out isNewContent),
                type);
        }

        public bool ContentExists(ContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));

            return contentReference.Chunks
                .Select(chunk => Repository.ValueExists(chunk.ContentUri))
                .Aggregate(true, (total, next) => total &= next);
        }

        public bool ContentValid(ContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));

            return contentReference.Chunks
                .Select(chunk => Repository.ValueValid(chunk.ContentUri))
                .Aggregate(true, (total, next) => total &= next);
        }

        public int AppendToLog(
            int newLogPosition,
            LogReference logReference)
        {
            return Repository.AppendToLog(
                newLogPosition,
                DataConvert.ToBytes(logReference));
        }

        public LogReference RetrieveFromLog(int logPosition)
        {
            return DataConvert.ToObject<LogReference>(
                Repository.RetrieveFromLog(logPosition));
        }

        private IImmutableList<ChunkReference> WriteChunks(
            byte[] key,
            byte[] nonce,
            Stream stream,
            out bool hasNewChunks)
        {
            var chunkedDataItems = _fastCdc.SplitIntoChunks(stream);
            var chunkReferences = ImmutableArray.CreateBuilder<ChunkReference>();
            hasNewChunks = false;

            foreach (var chunkedData in chunkedDataItems)
            {
                var (encryptedData, tag) = AesGcmCrypto.Encrypt(
                    chunkedData,
                    key,
                    nonce);

                // We add all cryptographic details needed to decrypt a piece of
                // content so that we can recover it even if a snapshot gets
                // corrupted.
                var value = nonce
                    .Concat(encryptedData)
                    .Concat(tag)
                    .ToArray();

                var contentUri = Repository.StoreValue(
                    _hashAlgorithmName,
                    value,
                    out var isNewValue);

                hasNewChunks |= isNewValue;
                chunkReferences.Add(new ChunkReference(contentUri, tag));
            }

            return chunkReferences.ToImmutable();
        }
    }
}
