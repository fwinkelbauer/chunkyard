using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Chunkyard
{
    internal class ContentStore : IContentStore
    {
        private const string DefaultLogName = "master";

        private readonly IRepository _repository;
        private readonly EncryptionProvider _encryptionProvider;
        private readonly FastCdc _fastCdc;
        private readonly HashAlgorithmName _hashAlgorithmName;

        private byte[]? _key;

        public ContentStore(
            IRepository repository,
            EncryptionProvider encryptionProvider,
            FastCdc fastCdc,
            HashAlgorithmName hashAlgorithmName)
        {
            _repository = repository;
            _encryptionProvider = encryptionProvider;
            _fastCdc = fastCdc;
            _hashAlgorithmName = hashAlgorithmName;
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
            Stream outputStream)
        {
            foreach (var chunk in contentReference.Chunks)
            {
                var decryptedData = AesGcmCrypto.Decrypt(
                    _repository.RetrieveContent(chunk.ContentUri),
                    chunk.Tag,
                    GenerateKey(),
                    chunk.Nonce);

                outputStream.Write(decryptedData);
            }
        }

        public T RetrieveContent<T>(ContentReference contentReference)
            where T : notnull
        {
            using var memoryStream = new MemoryStream();
            RetrieveContent(
                contentReference,
                memoryStream);

            return ToObject<T>(memoryStream.ToArray());
        }

        public ContentReference StoreContent(
            Stream inputStream,
            string contentName)
        {
            return new ContentReference(
                contentName,
                WriteChunks(inputStream));
        }

        public ContentReference StoreContent<T>(T value, string contentName)
            where T : notnull
        {
            using var memoryStream = new MemoryStream(ToBytes(value));
            return StoreContent(
                (Stream)memoryStream, contentName);
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

        public int AppendToLog<T>(T value, int? currentLogPosition)
            where T : notnull
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

        public IEnumerable<int> ListLogPositions()
        {
            return _repository.ListLogPositions(DefaultLogName);
        }

        private byte[] GenerateKey()
        {
            if (_key != null)
            {
                return _key;
            }

            var password = _encryptionProvider.Password
                ?? throw new ChunkyardException("No password was provided");

            _key = AesGcmCrypto.PasswordToKey(
                password,
                _encryptionProvider.Salt.ToArray(),
                _encryptionProvider.Iterations);

            return _key;
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

        private IEnumerable<ChunkReference> WriteChunks(Stream stream)
        {
            var chunkedDataItems = _fastCdc.SplitIntoChunks(stream);

            foreach (var chunkedData in chunkedDataItems)
            {
                var fingerprint = Id.ComputeHash(
                    _hashAlgorithmName,
                    chunkedData);

                var nonce = _encryptionProvider.GetNonce(fingerprint);

                var (encryptedData, tag) = AesGcmCrypto.Encrypt(
                    chunkedData,
                    GenerateKey(),
                    nonce);

                var contentUri = Id.ComputeContentUri(
                    _hashAlgorithmName,
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
