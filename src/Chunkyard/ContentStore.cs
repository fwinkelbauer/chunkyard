using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

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

        private int? _currentLogPosition;

        public ContentStore(
            IRepository repository,
            FastCdc fastCdc,
            HashAlgorithmName hashAlgorithmName,
            IPrompt prompt)
        {
            _repository = repository.EnsureNotNull(nameof(repository));
            _fastCdc = fastCdc;
            _hashAlgorithmName = hashAlgorithmName;

            CurrentLogPosition = FetchLogPosition(repository);
            string? password;

            prompt.EnsureNotNull(nameof(prompt));

            if (CurrentLogPosition == null)
            {
                password = prompt.NewPassword();
                _salt = AesGcmCrypto.GenerateSalt();
                _iterations = AesGcmCrypto.Iterations;
            }
            else
            {
                var logReference = RetrieveFromLog(
                    repository,
                    CurrentLogPosition.Value);

                password = prompt.ExistingPassword();
                _salt = logReference.Salt;
                _iterations = logReference.Iterations;
            }

            _key = AesGcmCrypto.PasswordToKey(password, _salt, _iterations);
        }

        public int? CurrentLogPosition
        {
            get
            {
                if (_currentLogPosition.HasValue)
                {
                    return _currentLogPosition;
                }

                _currentLogPosition = FetchLogPosition(_repository);

                return _currentLogPosition;
            }
            private set
            {
                _currentLogPosition = value;
            }
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
                    _repository.RetrieveValue(chunk.ContentUri),
                    chunk.Tag,
                    _key,
                    contentReference.Nonce);

                outputStream.Write(decryptedData);
            }
        }

        public StoreResult StoreContent(
            Stream inputStream,
            string contentName)
        {
            var nonce = AesGcmCrypto.GenerateNonce();

            var result = WriteChunks(nonce, inputStream);

            return new StoreResult(
                new ContentReference(
                    contentName,
                    nonce,
                    result.ChunkReferences),
                result.NewChunks);
        }

        public StoreResult StoreContent(
            Stream inputStream,
            ContentReference previousContentReference)
        {
            inputStream.EnsureNotNull(nameof(inputStream));
            previousContentReference.EnsureNotNull(
                nameof(previousContentReference));

            // Known files should be encrypted using the same nonce
            var nonce = previousContentReference.Nonce;

            var result = WriteChunks(nonce, inputStream);

            return new StoreResult(
                new ContentReference(
                    previousContentReference.Name,
                    nonce,
                    result.ChunkReferences),
                result.NewChunks);
        }

        public bool ContentExists(ContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));

            var exists = true;

            foreach (var chunk in contentReference.Chunks)
            {
                exists &= _repository.ValueExists(chunk.ContentUri);
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
            Guid logId,
            ContentReference contentReference,
            int newLogPosition)
        {
            var logReference = new LogReference(
                logId,
                contentReference,
                _salt,
                _iterations);

            CurrentLogPosition = _repository.AppendToLog(
                DataConvert.ToBytes(logReference),
                newLogPosition);

            return CurrentLogPosition.Value;
        }

        public LogReference RetrieveFromLog(int logPosition)
        {
            return RetrieveFromLog(
                _repository,
                ResolveLogPosition(logPosition));
        }

        public static LogReference RetrieveFromLog(
            IRepository repository,
            int logPosition)
        {
            repository.EnsureNotNull(nameof(repository));

            return DataConvert.ToObject<LogReference>(
                repository.RetrieveFromLog(logPosition));
        }

        private (IEnumerable<ChunkReference> ChunkReferences, bool NewChunks) WriteChunks(
            byte[] nonce,
            Stream stream)
        {
            var chunkedDataItems = _fastCdc.SplitIntoChunks(stream);
            var newChunks = false;
            var chunkReferences = new List<ChunkReference>();

            foreach (var chunkedData in chunkedDataItems)
            {
                var (encryptedData, tag) = AesGcmCrypto.Encrypt(
                    chunkedData,
                    _key,
                    nonce);

                var contentUri = Id.ComputeContentUri(
                    _hashAlgorithmName,
                    encryptedData);

                newChunks |= _repository.StoreValue(contentUri, encryptedData);

                chunkReferences.Add(new ChunkReference(contentUri, tag));
            }

            return (chunkReferences, newChunks);
        }

        private static int? FetchLogPosition(IRepository repository)
        {
            var logPositions = repository.ListLogPositions()
                .ToArray();

            if (logPositions.Length == 0)
            {
                return null;
            }

            return logPositions[^1];
        }

        private int ResolveLogPosition(int logPosition)
        {
            if (!CurrentLogPosition.HasValue)
            {
                throw new ChunkyardException(
                    "Cannot load snapshot from an empty repository");
            }

            //  0: the first element
            //  1: the second element
            // -1: the last element
            // -2: the second-last element
            return logPosition >= 0
                ? logPosition
                : CurrentLogPosition.Value + logPosition + 1;
        }
    }
}
