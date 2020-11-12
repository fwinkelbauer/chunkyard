using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;

namespace Chunkyard
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

        private int? _currentLogPosition;

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

        public int? CurrentLogPosition
        {
            get
            {
                if (_currentLogPosition.HasValue)
                {
                    return _currentLogPosition;
                }

                _currentLogPosition = FetchLogPosition(Repository);

                return _currentLogPosition;
            }
            private set => _currentLogPosition = value;
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
                    Repository.RetrieveValue(chunk.ContentUri),
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
            out bool newContent)
        {
            return new ContentReference(
                contentName,
                nonce,
                WriteChunks(nonce, inputStream, out newContent),
                type);
        }

        public bool ContentExists(ContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));
            var exists = true;

            foreach (var chunk in contentReference.Chunks)
            {
                exists &= Repository.ValueExists(chunk.ContentUri);
            }

            return exists;
        }

        public bool ContentValid(ContentReference contentReference)
        {
            contentReference.EnsureNotNull(nameof(contentReference));
            var valid = true;

            foreach (var chunk in contentReference.Chunks)
            {
                valid &= Repository.UriValid(chunk.ContentUri);
            }

            return valid;
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
            out bool newChunks)
        {
            var chunkedDataItems = _fastCdc.SplitIntoChunks(stream);
            var chunkReferences = ImmutableArray.CreateBuilder<ChunkReference>();
            newChunks = false;

            foreach (var chunkedData in chunkedDataItems)
            {
                var (encryptedData, tag) = AesGcmCrypto.Encrypt(
                    chunkedData,
                    _key,
                    nonce);

                var contentUri = Repository.StoreValue(
                    _hashAlgorithmName,
                    encryptedData,
                    out var newValue);

                newChunks |= newValue;
                chunkReferences.Add(new ChunkReference(contentUri, tag));
            }

            return chunkReferences.ToImmutable();
        }

        private static int? FetchLogPosition(IRepository repository)
        {
            var logPositions = repository.ListLogPositions();

            return logPositions.Length == 0
                ? null
                : logPositions[^1];
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
