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
        private readonly byte[] _salt;
        private readonly int _iterations;
        private readonly byte[] _key;

        public ContentStore(
            IRepository repository,
            FastCdc fastCdc,
            HashAlgorithmName hashAlgorithmName,
            IPrompt prompt)
        {
            Repository = repository.EnsureNotNull(nameof(repository));
            _fastCdc = fastCdc;
            _hashAlgorithmName = hashAlgorithmName;

            string? password;

            prompt.EnsureNotNull(nameof(prompt));

            var logPositions = repository.ListLogPositions();
            CurrentLogPosition = logPositions.Length == 0
                ? null
                : logPositions[^1];

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

        public IRepository Repository { get; }

        public int? CurrentLogPosition { get; private set; }

        public void RetrieveContent(
            ContentReference contentReference,
            Stream outputStream)
        {
            outputStream.EnsureNotNull(nameof(outputStream));
            contentReference.EnsureNotNull(nameof(contentReference));

            foreach (var chunk in contentReference.Chunks)
            {
                // Strip away the cryptographic details which we added when
                // storing the value
                var value = Repository.RetrieveValue(chunk.ContentUri)
                    .Skip(AesGcmCrypto.NonceBytes)
                    .SkipLast(AesGcmCrypto.TagBytes)
                    .ToArray();

                var decryptedData = AesGcmCrypto.Decrypt(
                    value,
                    chunk.Tag,
                    _key,
                    contentReference.Nonce);

                outputStream.Write(decryptedData);
            }
        }

        public ContentReference StoreContent(
            Stream inputStream,
            string contentName,
            byte[] nonce,
            ContentType type,
            out bool isNewContent)
        {
            return new ContentReference(
                contentName,
                nonce,
                WriteChunks(nonce, inputStream, out isNewContent),
                type);
        }

        public bool ContentExists(ContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));

            return contentReference.Chunks
                .Select(chunk => Repository.ValueExists(chunk.ContentUri))
                .Aggregate(true, (total, next) => total &= next);
        }

        public bool ContentValid(ContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));

            return contentReference.Chunks
                .Select(chunk => Repository.UriValid(chunk.ContentUri))
                .Aggregate(true, (total, next) => total &= next);
        }

        public int AppendToLog(
            int newLogPosition,
            ContentReference contentReference)
        {
            var logReference = new LogReference(
                contentReference,
                _salt,
                _iterations);

            CurrentLogPosition = Repository.AppendToLog(
                newLogPosition,
                DataConvert.ToBytes(logReference));

            return CurrentLogPosition.Value;
        }

        public LogReference RetrieveFromLog(int logPosition)
        {
            return RetrieveFromLog(
                Repository,
                ResolveLogPosition(logPosition));
        }

        private static LogReference RetrieveFromLog(
            IRepository repository,
            int logPosition)
        {
            repository.EnsureNotNull(nameof(repository));

            return DataConvert.ToObject<LogReference>(
                repository.RetrieveFromLog(logPosition));
        }

        private IImmutableList<ChunkReference> WriteChunks(
            byte[] nonce,
            Stream stream,
            out bool hasNewChunks)
        {
            var chunkedDataItems = _fastCdc.SplitIntoChunks(stream);
            var chunkReferences = ImmutableArray.CreateBuilder<ChunkReference>();
            hasNewChunks = false;

            foreach (var chunkedData in chunkedDataItems)
            {
                var (encryptedData, tag) = AesGcmCrypto.Encrypt(
                    chunkedData,
                    _key,
                    nonce);

                // We add all cryptographic details needed to decrypt a piece of
                // content so that we can recover it even if a snapshot gets
                // corrupted.
                var value = nonce
                    .Concat(encryptedData)
                    .Concat(tag)
                    .ToArray();

                var contentUri = Repository.StoreValue(
                    _hashAlgorithmName,
                    value,
                    out var isNewValue);

                hasNewChunks |= isNewValue;
                chunkReferences.Add(new ChunkReference(contentUri, tag));
            }

            return chunkReferences.ToImmutable();
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
