using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    /// <summary>
    /// An implementation of <see cref="IContentStore"/> which splits and
    /// encrypts files before storing them in an <see cref="IRepository{Uri}"/>.
    /// </summary>
    public class ContentStore : IContentStore
    {
        private readonly IRepository<Uri> _repository;
        private readonly FastCdc _fastCdc;
        private readonly HashAlgorithmName _hashAlgorithmName;

        public ContentStore(
            IRepository<Uri> repository,
            FastCdc fastCdc,
            HashAlgorithmName hashAlgorithmName)
        {
            _repository = repository.EnsureNotNull(nameof(repository));
            _fastCdc = fastCdc;
            _hashAlgorithmName = hashAlgorithmName;
        }

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
            try
            {
                foreach (var contentUri in contentReference.ContentUris)
                {
                    var decryptedData = AesGcmCrypto.Decrypt(
                        _repository.RetrieveValueValid(contentUri),
                        key);

                    outputStream.Write(decryptedData);
                }
            }
            catch (CryptographicException e)
            {
                throw new ChunkyardException("Could not decrypt data", e);
            }
        }

        public BlobReference StoreBlob(
            Blob blob,
            byte[] key,
            byte[] nonce,
            Stream inputStream)
        {
            blob.EnsureNotNull(nameof(blob));
            inputStream.EnsureNotNull(nameof(inputStream));

            return new BlobReference(
                blob.Name,
                blob.CreationTimeUtc,
                blob.LastWriteTimeUtc,
                nonce,
                WriteChunks(nonce, inputStream, key));
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
                .Select(contentUri => _repository.ValueExists(contentUri))
                .Aggregate(true, (total, next) => total & next);
        }

        public bool ContentValid(IContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));

            return contentReference.ContentUris
                .Select(contentUri => _repository.ValueValid(contentUri))
                .Aggregate(true, (total, next) => total & next);
        }

        public Uri[] ListContentUris()
        {
            return _repository.ListKeys();
        }

        public void RemoveContent(Uri contentUri)
        {
            _repository.RemoveValue(contentUri);
        }

        private Uri[] WriteChunks(
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

                var contentUri = _repository.StoreValue(
                    _hashAlgorithmName,
                    encryptedData);

                return contentUri;
            }

            if (_fastCdc.ExpectedChunkCount(stream.Length) > 100)
            {
                return _fastCdc.SplitIntoChunks(stream)
                    .AsParallel()
                    .Select(WriteChunk)
                    .ToArray();
            }
            else
            {
                return _fastCdc.SplitIntoChunks(stream)
                    .Select(WriteChunk)
                    .ToArray();
            }
        }
    }
}
