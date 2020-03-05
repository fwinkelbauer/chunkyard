using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Chunkyard.Core;
using Newtonsoft.Json;
using Serilog;

namespace Chunkyard
{
    internal class SnapshotBuilder
    {
        private const string SnapshotContentName = "snapshot";

        private static readonly ILogger _log = Log.ForContext<SnapshotBuilder>();

        private readonly IRepository _repository;
        private readonly HashAlgorithmName _hashAlgorithmName;
        private readonly string _password;
        private readonly int _minChunkSizeInByte;
        private readonly int _avgChunkSizeInByte;
        private readonly int _maxChunkSizeInByte;
        private readonly string _cacheDirectory;
        private readonly string _tempDirectory;
        private readonly List<(Func<Stream>, string)> _contentItems;
        private readonly Dictionary<string, byte[]> _noncesByName;

        private byte[] _key;

        public SnapshotBuilder(IRepository repository, HashAlgorithmName hashAlgorithmName, string password, int minChunkSizeInByte, int avgChunkSizeInByte, int maxChunkSizeInByte, string directory)
        {
            _repository = repository;
            _hashAlgorithmName = hashAlgorithmName;
            _password = password;
            _minChunkSizeInByte = minChunkSizeInByte;
            _avgChunkSizeInByte = avgChunkSizeInByte;
            _maxChunkSizeInByte = maxChunkSizeInByte;
            _cacheDirectory = Path.Join(directory, "cache");
            _tempDirectory = Path.Join(directory, "tmp");
            _contentItems = new List<(Func<Stream>, string)>();
            _noncesByName = new Dictionary<string, byte[]>();

            _key = Array.Empty<byte>();
        }

        public void AddContent(Func<Stream> readFunc, string contentName)
        {
            _contentItems.Add((readFunc, contentName));
        }

        public int WriteSnapshot(string logName, DateTime creationTime)
        {
            var currentLogPosition = _repository.FetchLogPosition(logName);

            var salt = AesGcmCrypto.GenerateSalt();
            var iterations = 1000;
            InitializeKey(salt, iterations);

            if (currentLogPosition.HasValue)
            {
                var currentSnapshotRoot = _repository.RetrieveFromLog<ContentRoot>(logName, currentLogPosition.Value);
                salt = currentSnapshotRoot.Salt;
                iterations = currentSnapshotRoot.Iterations;
                InitializeKey(salt, iterations);
                var currentSnapshot = ParseSnapshot(currentSnapshotRoot.ContentReference);

                foreach (var contentReference in currentSnapshot.ContentReferences)
                {
                    // Known files should be encrypted using the existing
                    // parameters, so we register all previous references
                    _noncesByName[contentReference.ContentName] = contentReference.Nonce;
                }
            }

            var snapshot = new Snapshot(creationTime, WriteContentItems());
            using var snapshotBuffer = new MemoryStream(
                ByteSerialize(snapshot));

            var nonce = GetNonce(SnapshotContentName);
            var snapshotRoot = new ContentRoot(
                new ContentReference(
                    SnapshotContentName,
                    nonce,
                    WriteStream(snapshotBuffer, nonce)),
                salt,
                iterations);

            return _repository.AppendToLog(
                ByteSerialize(snapshotRoot),
                logName,
                currentLogPosition);
        }

        public void Restore(Uri snapshotUri, Func<string, Stream> writeFunc, string restoreRegex)
        {
            var snapshot = RetrieveSnapshot(snapshotUri);
            var compiledRegex = new Regex(restoreRegex);

            foreach (var contentReference in snapshot.ContentReferences)
            {
                if (compiledRegex.IsMatch(contentReference.ContentName))
                {
                    _log.Information("Restoring: {File}", contentReference.ContentName);
                    using var stream = writeFunc(contentReference.ContentName);
                    Read(contentReference, stream);
                }
            }
        }

        public void VerifySnapshot(Uri snapshotUri)
        {
            var snapshot = RetrieveSnapshot(snapshotUri);

            foreach (var contentReference in snapshot.ContentReferences)
            {
                _log.Information("Verifying: {File}", contentReference.ContentName);

                foreach (var chunk in contentReference.Chunks)
                {
                    _repository.ThrowIfInvalid(chunk.ContentUri);
                }
            }
        }

        public IEnumerable<string> List(Uri snapshotUri, string listRegex)
        {
            var snapshot = RetrieveSnapshot(snapshotUri);
            var compiledRegex = new Regex(listRegex);

            foreach (var contentReferences in snapshot.ContentReferences)
            {
                if (compiledRegex.IsMatch(contentReferences.ContentName))
                {
                    yield return contentReferences.ContentName;
                }
            }
        }

        public void Push(string logName, IRepository destinationRepository)
        {
            var sourceLogPosition = _repository.FetchLogPosition(logName);
            var destinationLogPosition = destinationRepository.FetchLogPosition(logName);

            if (!sourceLogPosition.HasValue)
            {
                _log.Information("Cannot push an empty log");
                return;
            }

            var commonLogPosition = destinationLogPosition.HasValue
                ? Math.Min(sourceLogPosition.Value, destinationLogPosition.Value)
                : -1;

            if (commonLogPosition == sourceLogPosition)
            {
                _log.Information("Already up-to-date");
                return;
            }

            for (int i = 0; i <= commonLogPosition; i++)
            {
                var source = _repository.RetrieveFromLog(logName, i);
                var destination = destinationRepository.RetrieveFromLog(logName, i);

                if (!Enumerable.SequenceEqual(source, destination))
                {
                    throw new ChunkyardException($"Logs differ at position {i}");
                }
            }

            for (int i = commonLogPosition + 1; i <= sourceLogPosition; i++)
            {
                _log.Information("Pushing snapshot with position: {LogPosition}", i);

                var snapshotRoot = _repository.RetrieveFromLog<ContentRoot>(logName, i);
                InitializeKey(snapshotRoot.Salt, snapshotRoot.Iterations);
                PushSnapshot(snapshotRoot.ContentReference, destinationRepository);
                destinationRepository.AppendToLog(
                    ByteSerialize(snapshotRoot),
                    logName,
                    i - 1);
            }
        }

        private void InitializeKey(byte[] salt, int iterations)
        {
            _key = AesGcmCrypto.PasswordToKey(
                _password,
                salt,
                iterations);
        }

        private Snapshot RetrieveSnapshot(Uri snapshotUri)
        {
            var snapshotRoot = _repository.RetrieveFromLog<ContentRoot>(snapshotUri);
            InitializeKey(snapshotRoot.Salt, snapshotRoot.Iterations);
            return ParseSnapshot(snapshotRoot.ContentReference);
        }

        private Snapshot ParseSnapshot(ContentReference snapshotReference)
        {
            using var memoryBuffer = new MemoryStream();
            Read(snapshotReference, memoryBuffer);

            return Deserialize<Snapshot>(memoryBuffer.ToArray());
        }

        private static byte[] ByteSerialize(object o)
        {
            return Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(o));
        }

        private static T Deserialize<T>(byte[] value)
        {
            return JsonConvert.DeserializeObject<T>(
                Encoding.UTF8.GetString(value));
        }

        private IEnumerable<ContentReference> WriteContentItems()
        {
            foreach (var (readFunc, contentName) in _contentItems)
            {
                yield return WriteContentItem(readFunc, contentName);
            }
        }

        private ContentReference WriteContentItem(Func<Stream> readFunc, string contentName)
        {
            _log.Information("Storing: {Content}", contentName);

            using var stream = readFunc();
            var nonce = GetNonce(contentName);

            if (!(stream is FileStream fileStream))
            {
                return new ContentReference(
                    contentName,
                    nonce,
                    WriteStream(stream, nonce));
            }

            var storedCache = RetrieveFromCache(contentName);

            if (storedCache != null
                && storedCache.Length == fileStream.Length
                && storedCache.CreationDateUtc.Equals(File.GetCreationTimeUtc(fileStream.Name))
                && storedCache.LastWriteDateUtc.Equals(File.GetLastWriteTimeUtc(fileStream.Name)))
            {
                return storedCache.ContentReference;
            }

            var contentReference = new ContentReference(
                contentName,
                nonce,
                WriteStream(stream, nonce));

            StoreInCache(
                contentName,
                new Cache(
                    contentReference,
                    fileStream.Length,
                    File.GetCreationTimeUtc(fileStream.Name),
                    File.GetLastWriteTimeUtc(fileStream.Name)));

            return contentReference;
        }

        private IEnumerable<Chunk> WriteStream(Stream stream, byte[] nonce)
        {
            var chunkedDataItems = FastCdc.SplitIntoChunks(
                stream,
                _minChunkSizeInByte,
                _avgChunkSizeInByte,
                _maxChunkSizeInByte,
                _tempDirectory);

            foreach (var chunkedData in chunkedDataItems)
            {
                var (encryptedData, tag) = AesGcmCrypto.Encrypt(chunkedData, _key, nonce);
                var compressedData = LzmaCompression.Compress(encryptedData);

                yield return new Chunk(
                    _repository.StoreContent(_hashAlgorithmName, compressedData),
                    tag);
            }
        }

        private void PushSnapshot(ContentReference snapshotReference, IRepository remoteRepository)
        {
            var snapshot = ParseSnapshot(snapshotReference);

            foreach (var contentReferences in snapshot.ContentReferences)
            {
                _log.Information("Pushing content: {Content}", contentReferences.ContentName);

                foreach (var chunk in contentReferences.Chunks)
                {
                    _repository.PushContent(chunk.ContentUri, remoteRepository);
                }
            }

            foreach (var chunk in snapshotReference.Chunks)
            {
                _repository.PushContent(chunk.ContentUri, remoteRepository);
            }
        }

        private void Read(ContentReference contentReference, Stream stream)
        {
            foreach (var chunk in contentReference.Chunks)
            {
                var compressedData = _repository.RetrieveContentChecked(chunk.ContentUri);
                var decompressedData = LzmaCompression.Decompress(compressedData);
                var decryptedData = AesGcmCrypto.Decrypt(
                    decompressedData,
                    chunk.Tag,
                    _key,
                    contentReference.Nonce);

                stream.Write(decryptedData);
            }
        }

        private byte[] GetNonce(string name)
        {
            if (_noncesByName.TryGetValue(name, out var nonce))
            {
                return nonce;
            }
            else
            {
                nonce = AesGcmCrypto.GenerateNonce();
                _noncesByName[name] = nonce;

                return nonce;
            }
        }

        private Cache? RetrieveFromCache(string contentName)
        {
            var cacheFile = ToCacheFile(contentName);

            if (!File.Exists(cacheFile))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<Cache>(
                File.ReadAllText(cacheFile));
        }

        private void StoreInCache(string contentName, Cache cache)
        {
            var cacheFile = ToCacheFile(contentName);

            Directory.CreateDirectory(Path.GetDirectoryName(cacheFile));

            File.WriteAllText(
                cacheFile,
                JsonConvert.SerializeObject(cache));
        }

        private string ToCacheFile(string contentName)
        {
            return Path.Combine(_cacheDirectory, contentName);
        }

        private class Cache
        {
            public Cache(ContentReference contentReference, long length, DateTime creationDateUtc, DateTime lastWriteDateUtc)
            {
                ContentReference = contentReference;
                Length = length;
                CreationDateUtc = creationDateUtc;
                LastWriteDateUtc = lastWriteDateUtc;
            }

            public ContentReference ContentReference { get; }

            public long Length { get; }

            public DateTime CreationDateUtc { get; }

            public DateTime LastWriteDateUtc { get; }
        }
    }
}
