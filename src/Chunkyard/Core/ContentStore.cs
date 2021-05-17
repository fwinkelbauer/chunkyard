using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    /// <summary>
    /// A store which splits and encrypts files before storing them in an
    /// <see cref="IRepository{Uri}"/>.
    /// </summary>
    public class ContentStore
    {
        private readonly IRepository<Uri> _repository;
        private readonly FastCdc _fastCdc;
        private readonly string _hashAlgorithmName;
        private readonly IProbe _probe;

        public ContentStore(
            IRepository<Uri> repository,
            FastCdc fastCdc,
            string hashAlgorithmName,
            IProbe probe)
        {
            _repository = repository.EnsureNotNull(nameof(repository));
            _fastCdc = fastCdc;
            _hashAlgorithmName = hashAlgorithmName;
            _probe = probe;
        }

        public void RetrieveBlob(
            BlobReference blobReference,
            byte[] key,
            Stream outputStream)
        {
            blobReference.EnsureNotNull(nameof(blobReference));
            outputStream.EnsureNotNull(nameof(outputStream));

            RetrieveContent(blobReference, key, outputStream);

            _probe.RetrievedBlob(blobReference.Name);
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
                    var decrypted = AesGcmCrypto.Decrypt(
                        RetrieveValid(contentUri),
                        key);

                    outputStream.Write(decrypted);
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

            var blobReference = new BlobReference(
                blob.Name,
                blob.CreationTimeUtc,
                blob.LastWriteTimeUtc,
                nonce,
                WriteChunks(nonce, inputStream, key));

            _probe.StoredBlob(blobReference.Name);

            return blobReference;
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

            var exists = contentReference.ContentUris
                .Select(contentUri => _repository.ValueExists(contentUri))
                .Aggregate(true, (total, next) => total & next);

            if (contentReference is BlobReference blobReference)
            {
                if (exists)
                {
                    _probe.BlobExists(blobReference.Name);
                }
                else
                {
                    _probe.BlobMissing(blobReference.Name);
                }
            }

            return exists;
        }

        public bool ContentValid(IContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));

            var valid = contentReference.ContentUris
                .Select(contentUri =>
                {
                    return _repository.ValueExists(contentUri)
                        && Id.ContentUriValid(
                            contentUri,
                            _repository.RetrieveValue(contentUri));
                })
                .Aggregate(true, (total, next) => total & next);

            if (contentReference is BlobReference blobReference)
            {
                if (valid)
                {
                    _probe.BlobValid(blobReference.Name);
                }
                else
                {
                    _probe.BlobInvalid(blobReference.Name);
                }
            }

            return valid;
        }

        public void RemoveExcept(IEnumerable<Uri> usedContentUris)
        {
            var unusedContentUris = _repository.ListKeys()
                .Except(usedContentUris)
                .ToArray();

            foreach (var contentUri in unusedContentUris)
            {
                _repository.RemoveValue(contentUri);

                _probe.RemovedContent(contentUri);
            }
        }

        public void Copy(IRepository<Uri> repository)
        {
            repository.EnsureNotNull(nameof(repository));

            var contentUrisToCopy = _repository.ListKeys()
                .Except(repository.ListKeys())
                .ToArray();

            foreach (var contentUri in contentUrisToCopy)
            {
                repository.StoreValue(
                    contentUri,
                    RetrieveValid(contentUri));

                _probe.CopiedContent(contentUri);
            }
        }

        private byte[] RetrieveValid(Uri contentUri)
        {
            var value = _repository.RetrieveValue(contentUri);

            if (!Id.ContentUriValid(contentUri, value))
            {
                throw new ChunkyardException(
                    $"Invalid content: {contentUri}");
            }

            return value;
        }

        private Uri[] WriteChunks(
            byte[] nonce,
            Stream stream,
            byte[] key)
        {
            return _fastCdc.SplitIntoChunks(stream)
                .Select(chunk =>
                {
                    var encryptedData = AesGcmCrypto.Encrypt(
                        nonce,
                        chunk.Value,
                        key);

                    var contentUri = Id.ComputeContentUri(
                        _hashAlgorithmName,
                        encryptedData);

                    _repository.StoreValue(contentUri, encryptedData);

                    return contentUri;
                })
                .ToArray();
        }
    }
}
