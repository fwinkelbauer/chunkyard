using System;
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
            foreach (var contentUri in contentReference.ContentUris)
            {
                var decryptedData = AesGcmCrypto.Decrypt(
                    Repository.RetrieveValue(contentUri),
                    key);

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
                WriteChunks(nonce, stream, key));
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
                WriteChunks(nonce, memoryStream, key));
        }

        public bool ContentExists(IContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));

            return contentReference.ContentUris
                .Select(contentUri => Repository.ValueExists(contentUri))
                .Aggregate(true, (total, next) => total &= next);
        }

        public bool ContentValid(IContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));

            return contentReference.ContentUris
                .Select(contentUri => Repository.ValueValid(contentUri))
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

        private IImmutableList<Uri> WriteChunks(
            byte[] nonce,
            Stream stream,
            byte[] key)
        {
            Uri WriteChunk(byte[] chunk)
            {
                var encryptedData = AesGcmCrypto.Encrypt(
                    nonce,
                    chunk,
                    key);

                var contentUri = Repository.StoreValue(
                    _hashAlgorithmName,
                    encryptedData);

                return contentUri;
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
