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

        public void RetrieveBlob(
            BlobReference blobReference,
            byte[] key,
            Stream outputStream)
        {
            blobReference.EnsureNotNull(nameof(blobReference));
            outputStream.EnsureNotNull(nameof(outputStream));

            RetrieveContent(blobReference, key, outputStream);
        }

        public T RetrieveDocument<T>(
            DocumentReference documentReference,
            byte[] key)
            where T : notnull
        {
            documentReference.EnsureNotNull(nameof(documentReference));

            using var memoryStream = new MemoryStream();

            RetrieveContent(
                documentReference,
                key,
                memoryStream);

            return DataConvert.ToObject<T>(memoryStream.ToArray());
        }

        private void RetrieveContent(
            IContentReference contentReference,
            byte[] key,
            Stream outputStream)
        {
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

        public BlobReference StoreBlob(
            Blob blob,
            byte[] key,
            byte[] nonce)
        {
            blob.EnsureNotNull(nameof(blob));

            var stream = blob.OpenRead();

            return new BlobReference(
                blob.Name,
                blob.CreationTimeUtc,
                blob.LastWriteTimeUtc,
                nonce,
                WriteChunks(key, nonce, stream));
        }

        public DocumentReference StoreDocument<T>(
            T value,
            byte[] key,
            byte[] nonce)
            where T : notnull
        {
            using var memoryStream = new MemoryStream(
                DataConvert.ToBytes(value));

            return new DocumentReference(
                nonce,
                WriteChunks(key, nonce, memoryStream));
        }

        public bool ContentExists(IContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));

            return contentReference.Chunks
                .Select(chunk => Repository.ValueExists(chunk.ContentUri))
                .Aggregate(true, (total, next) => total &= next);
        }

        public bool ContentValid(IContentReference contentReference)
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
            Stream stream)
        {
            ChunkReference WriteChunk(byte[] chunk)
            {
                var (encryptedData, tag) = AesGcmCrypto.Encrypt(
                    chunk,
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
                    value);

                return new ChunkReference(
                    contentUri,
                    tag);
            }

            if (_fastCdc.ExpectedChunkCount(stream.Length) > 100)
            {
                return _fastCdc.SplitIntoChunks(stream)
                    .AsParallel()
                    .Select(WriteChunk)
                    .ToImmutableArray();
            }
            else
            {
                return _fastCdc.SplitIntoChunks(stream)
                    .Select(WriteChunk)
                    .ToImmutableArray();
            }
        }
    }
}
