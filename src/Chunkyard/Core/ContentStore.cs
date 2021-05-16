using System;
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
        private readonly FastCdc _fastCdc;
        private readonly string _hashAlgorithmName;

        public ContentStore(
            IRepository<Uri> repository,
            FastCdc fastCdc,
            string hashAlgorithmName)
        {
            Repository = repository.EnsureNotNull(nameof(repository));

            _fastCdc = fastCdc;
            _hashAlgorithmName = hashAlgorithmName;
        }

        public IRepository<Uri> Repository { get; }

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
                    var content = Repository.RetrieveValue(contentUri);

                    if (!Id.ContentUriValid(contentUri, content))
                    {
                        throw new ChunkyardException(
                            $"Invalid content: {contentUri}");
                    }

                    var decrypted = AesGcmCrypto.Decrypt(content, key);

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
                .Select(contentUri => Repository.ValueExists(contentUri))
                .Aggregate(true, (total, next) => total & next);
        }

        public bool ContentValid(IContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));

            return contentReference.ContentUris
                .Select(contentUri =>
                {
                    return Repository.ValueExists(contentUri)
                        && Id.ContentUriValid(
                            contentUri,
                            Repository.RetrieveValue(contentUri));
                })
                .Aggregate(true, (total, next) => total & next);
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

                    Repository.StoreValue(contentUri, encryptedData);

                    return contentUri;
                })
                .ToArray();
        }
    }
}
