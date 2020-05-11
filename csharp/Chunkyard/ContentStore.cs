using System;
using System.Collections.Generic;
using System.IO;
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

        public ContentStore(
            IRepository repository,
            NonceGenerator nonceGenerator,
            ContentStoreConfig config)
        {
            _repository = repository;
            _nonceGenerator = nonceGenerator;
            _config = config;
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
            byte[] key,
            Stream outputStream)
        {
            foreach (var chunk in contentReference.Chunks)
            {
                var decryptedData = AesGcmCrypto.Decrypt(
                    _repository.RetrieveContent(chunk.ContentUri),
                    chunk.Tag,
                    key,
                    chunk.Nonce);

                outputStream.Write(decryptedData);
            }
        }

        public T RetrieveContent<T>(
            ContentReference contentReference,
            byte[] key) where T : notnull
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
            byte[] key,
            string contentName)
        {
            return new ContentReference(
                contentName,
                WriteChunks(inputStream, key));
        }

        public ContentReference StoreContent<T>(
            T value,
            byte[] key,
            string contentName) where T : notnull
        {
            using var memoryStream = new MemoryStream(ToBytes(value));
            return new ContentReference(
                contentName,
                WriteChunks(memoryStream, key));
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

        public int AppendToLog<T>(
            T value,
            int? currentLogPosition) where T : notnull
        {
            return _repository.AppendToLog(
                ToBytes(value),
                DefaultLogName,
                currentLogPosition);
        }

        public T RetrieveFromLog<T>(int logPosition) where T : notnull
        {
            return ToObject<T>(
                _repository.RetrieveFromLog(DefaultLogName, logPosition));
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

        private IEnumerable<Chunk> WriteChunks(Stream stream, byte[] key)
        {
            var chunkedDataItems = FastCdc.SplitIntoChunks(
                stream,
                _config.MinChunkSizeInByte,
                _config.AvgChunkSizeInByte,
                _config.MaxChunkSizeInByte);

            foreach (var chunkedData in chunkedDataItems)
            {
                var fingerprint = Id.ComputeHash(
                    _config.HashAlgorithmName,
                    chunkedData);

                var nonce = _nonceGenerator.GetNonce(fingerprint);

                var (encryptedData, tag) = AesGcmCrypto.Encrypt(
                    chunkedData,
                    key,
                    nonce);

                var contentUri = Id.ComputeContentUri(
                    _config.HashAlgorithmName,
                    encryptedData);

                _repository.StoreContent(contentUri, encryptedData);

                yield return new Chunk(
                    contentUri,
                    fingerprint,
                    nonce,
                    tag);
            }
        }
    }
}
