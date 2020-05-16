using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Chunkyard
{
    internal class ContentStore : IContentStore
    {
        private const string DefaultLogName = "master";

        private readonly IRepository _repository;
        private readonly NonceGenerator _nonceGenerator;
        private readonly ContentStoreConfig _config;
        private readonly FastCdc _fastCdc;

        public ContentStore(
            IRepository repository,
            NonceGenerator nonceGenerator,
            ContentStoreConfig config)
        {
            _repository = repository;
            _nonceGenerator = nonceGenerator;
            _config = config;
            _fastCdc = new FastCdc(
                _config.MinChunkSizeInByte,
                _config.AvgChunkSizeInByte,
                _config.MaxChunkSizeInByte);
        }

        public Uri StoreUri
        {
            get
            {
                return _repository.RepositoryUri;
            }
        }

        public void RetrieveContent(
            ContentReference contentReference,
            KeyInformation key,
            Stream outputStream)
        {
            foreach (var chunk in contentReference.Chunks)
            {
                var decryptedData = AesGcmCrypto.Decrypt(
                    _repository.RetrieveContent(chunk.ContentUri),
                    chunk.Tag,
                    key.Key,
                    chunk.Nonce);

                outputStream.Write(decryptedData);
            }
        }

        public T RetrieveContent<T>(
            ContentReference contentReference,
            KeyInformation key) where T : notnull
        {
            using var memoryStream = new MemoryStream();
            RetrieveContent(
                contentReference,
                key,
                memoryStream);

            return ToObject<T>(memoryStream.ToArray());
        }

        public ContentReference StoreContent(
            Stream inputStream,
            KeyInformation key,
            string contentName)
        {
            return new ContentReference(
                contentName,
                WriteChunks(inputStream, key),
                key.Salt,
                key.Iterations);
        }

        public ContentReference StoreContent<T>(
            T value,
            KeyInformation key,
            string contentName) where T : notnull
        {
            using var memoryStream = new MemoryStream(ToBytes(value));
            return new ContentReference(
                contentName,
                WriteChunks(memoryStream, key),
                key.Salt,
                key.Iterations);
        }

        public bool ContentExists(ContentReference contentReference)
        {
            var exists = true;

            foreach (var chunk in contentReference.Chunks)
            {
                exists &= _repository.ContentExists(chunk.ContentUri);
            }

            return exists;
        }

        public bool ContentValid(ContentReference contentReference)
        {
            var valid = true;

            foreach (var chunk in contentReference.Chunks)
            {
                valid &= _repository.ContentValid(chunk.ContentUri);
            }

            return valid;
        }

        public int? FetchLogPosition()
        {
            return _repository.FetchLogPosition(DefaultLogName);
        }

        public int AppendToLog(
            ContentReference contentReference,
            int? currentLogPosition)
        {
            // We do not want to leak any fingerprints in an unencrypted
            // reference
            var safeContentReference = new ContentReference(
                contentReference.Name,
                contentReference.Chunks.Select(
                    c => new ChunkReference(
                        c.ContentUri,
                        string.Empty,
                        c.Nonce,
                        c.Tag)),
                contentReference.Salt,
                contentReference.Iterations);

            return _repository.AppendToLog(
                ToBytes(safeContentReference),
                DefaultLogName,
                currentLogPosition);
        }

        public ContentReference RetrieveFromLog(int logPosition)
        {
            return ToObject<ContentReference>(
                _repository.RetrieveFromLog(DefaultLogName, logPosition));
        }

        public IEnumerable<int> ListLogPositions()
        {
            return _repository.ListLogPositions(DefaultLogName);
        }

        private static byte[] ToBytes(object o)
        {
            return Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(o));
        }

        private static T ToObject<T>(byte[] value) where T : notnull
        {
            return JsonConvert.DeserializeObject<T>(
                Encoding.UTF8.GetString(value));
        }

        private IEnumerable<ChunkReference> WriteChunks(
            Stream stream,
            KeyInformation key)
        {
            var chunkedDataItems = _fastCdc.SplitIntoChunks(stream);

            foreach (var chunkedData in chunkedDataItems)
            {
                var fingerprint = Id.ComputeHash(
                    _config.HashAlgorithmName,
                    chunkedData);

                var nonce = _nonceGenerator.GetNonce(fingerprint);

                var (encryptedData, tag) = AesGcmCrypto.Encrypt(
                    chunkedData,
                    key.Key,
                    nonce);

                var contentUri = Id.ComputeContentUri(
                    _config.HashAlgorithmName,
                    encryptedData);

                _repository.StoreContent(contentUri, encryptedData);

                yield return new ChunkReference(
                    contentUri,
                    fingerprint,
                    nonce,
                    tag);
            }
        }
    }
}
