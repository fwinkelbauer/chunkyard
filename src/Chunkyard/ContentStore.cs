using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Chunkyard
{
    /// <summary>
    /// An implementation of <see cref="IContentStore"/> which splits and
    /// encrypts files before storing them in an <see cref="IRepository"/>.
    /// </summary>
    public class ContentStore : IContentStore
    {
        private readonly IRepository _repository;
        private readonly FastCdc _fastCdc;
        private readonly HashAlgorithmName _hashAlgorithmName;
        private readonly byte[] _salt;
        private readonly int _iterations;
        private readonly byte[] _key;

        public ContentStore(
            IRepository repository,
            FastCdc fastCdc,
            HashAlgorithmName hashAlgorithmName,
            string password,
            byte[] salt,
            int iterations)
        {
            _repository = repository;
            _fastCdc = fastCdc;
            _hashAlgorithmName = hashAlgorithmName;
            _salt = salt;
            _iterations = iterations;
            _key = AesGcmCrypto.PasswordToKey(password, salt, iterations);
        }

        public void RetrieveContent(
            ContentReference contentReference,
            Stream outputStream)
        {
            outputStream.EnsureNotNull(nameof(outputStream));
            contentReference.EnsureNotNull(nameof(contentReference));

            foreach (var chunk in contentReference.Chunks)
            {
                var decryptedData = AesGcmCrypto.Decrypt(
                    _repository.RetrieveUri(chunk.ContentUri),
                    chunk.Tag,
                    _key,
                    contentReference.Nonce);

                outputStream.Write(decryptedData, 0, chunk.Length);
            }
        }

        public T RetrieveContentObject<T>(ContentReference contentReference)
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
            var nonce = AesGcmCrypto.GenerateNonce();

            return new ContentReference(
                contentName,
                nonce,
                WriteChunks(nonce, inputStream));
        }

        public ContentReference StoreContent(
            Stream inputStream,
            ContentReference previousContentReference)
        {
            inputStream.EnsureNotNull(nameof(inputStream));
            previousContentReference.EnsureNotNull(
                nameof(previousContentReference));

            // Known files should be encrypted using the same nonce
            var nonce = previousContentReference.Nonce;

            return new ContentReference(
                previousContentReference.Name,
                nonce,
                WriteChunks(nonce, inputStream));
        }

        public ContentReference StoreContentObject<T>(
            T value,
            string contentName)
            where T : notnull
        {
            using var memoryStream = new MemoryStream(ToBytes(value));

            return StoreContent(
                memoryStream,
                contentName);
        }

        public ContentReference StoreContentObject<T>(
            T value,
            ContentReference previousContentReference)
            where T : notnull
        {
            using var memoryStream = new MemoryStream(ToBytes(value));

            return StoreContent(
                memoryStream,
                previousContentReference);
        }

        public bool ContentExists(ContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));

            var exists = true;

            foreach (var chunk in contentReference.Chunks)
            {
                exists &= _repository.UriExists(chunk.ContentUri);
            }

            return exists;
        }

        public bool ContentValid(ContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));

            var valid = true;

            foreach (var chunk in contentReference.Chunks)
            {
                valid &= _repository.UriValid(chunk.ContentUri);
            }

            return valid;
        }

        public int AppendToLog(
            ContentReference contentReference,
            int newLogPosition)
        {
            var logReference = new LogReference(
                contentReference,
                _salt,
                _iterations);

            return _repository.AppendToLog(
                ToBytes(logReference),
                newLogPosition);
        }

        public LogReference RetrieveFromLog(int logPosition)
        {
            return RetrieveFromLog(_repository, logPosition);
        }

        public static LogReference RetrieveFromLog(
            IRepository repository,
            int logPosition)
        {
            repository.EnsureNotNull(nameof(repository));

            return ToObject<LogReference>(
                repository.RetrieveFromLog(logPosition));
        }

        private IEnumerable<ChunkReference> WriteChunks(
            byte[] nonce,
            Stream stream)
        {
            var chunkedDataItems = _fastCdc.SplitIntoChunks(stream);

            foreach (var chunkedData in chunkedDataItems)
            {
                using var chunkedStream = new MemoryStream();
                chunkedStream.Write(chunkedData);
                var missingLength = _fastCdc.MaxSize - chunkedData.Length;

                if (missingLength > 0)
                {
                    chunkedStream.Write(
                        AesGcmCrypto.GenerateRandomMumber(missingLength));
                }

                var (encryptedData, tag) = AesGcmCrypto.Encrypt(
                    chunkedStream.ToArray(),
                    _key,
                    nonce);

                var contentUri = Id.ComputeContentUri(
                    _hashAlgorithmName,
                    encryptedData);

                _repository.StoreUri(contentUri, encryptedData);

                yield return new ChunkReference(
                    contentUri,
                    chunkedData.Length,
                    tag);
            }
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
    }
}
