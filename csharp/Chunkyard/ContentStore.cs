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

        public ContentStore(IRepository repository)
        {
            _repository = repository;
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
            RetrieveConfig retrieveConfig,
            Stream outputStream)
        {
            foreach (var chunk in contentReference.Chunks)
            {
                var decryptedData = AesGcmCrypto.Decrypt(
                    _repository.RetrieveContent(chunk.ContentUri),
                    chunk.Tag,
                    retrieveConfig.Key,
                    chunk.Nonce);

                outputStream.Write(decryptedData);
            }
        }

        public T RetrieveContent<T>(
            ContentReference contentReference,
            RetrieveConfig retrieveConfig) where T : notnull
        {
            using var memoryStream = new MemoryStream();
            RetrieveContent(
                contentReference,
                retrieveConfig,
                memoryStream);

            return ToObject<T>(memoryStream.ToArray());
        }

        // TODO StoreConfig vereinfachen, ContentName direkt als Param?
        public ContentReference StoreContent(
            Stream inputStream,
            StoreConfig storeConfig)
        {
            return new ContentReference(
                storeConfig.ContentName,
                WriteChunks(inputStream, storeConfig));
        }

        public ContentReference StoreContent<T>(
            T value,
            StoreConfig storeConfig) where T : notnull
        {
            using var memoryStream = new MemoryStream(ToBytes(value));
            return new ContentReference(
                storeConfig.ContentName,
                WriteChunks(memoryStream, storeConfig));
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

        private IEnumerable<Chunk> WriteChunks(
            Stream stream,
            StoreConfig storeConfig)
        {
            var chunkedDataItems = FastCdc.SplitIntoChunks(
                stream,
                storeConfig.MinChunkSizeInByte,
                storeConfig.AvgChunkSizeInByte,
                storeConfig.MaxChunkSizeInByte);

            foreach (var chunkedData in chunkedDataItems)
            {
                var fingerprint = Id.ComputeHash(
                    storeConfig.HashAlgorithmName,
                    chunkedData);

                var nonce = storeConfig.NonceGenerator(fingerprint);

                var (encryptedData, tag) = AesGcmCrypto.Encrypt(
                    chunkedData,
                    storeConfig.Key,
                    nonce);

                var contentUri = Id.ComputeContentUri(
                    storeConfig.HashAlgorithmName,
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
