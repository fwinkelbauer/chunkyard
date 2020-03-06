using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Chunkyard.Core;
using Serilog;

namespace Chunkyard
{
    internal class SnapshotBuilder
    {
        private const string SnapshotContentName = "snapshot";
        private const int Iterations = 1000;

        private static readonly ILogger _log = Log.ForContext<SnapshotBuilder>();

        private readonly IContentStore _contentStore;
        private readonly string _password;
        private readonly List<(Func<Stream>, string)> _contentItems;
        private readonly Dictionary<string, byte[]> _noncesByName;

        private byte[] _key;

        public SnapshotBuilder(IContentStore contentStore, string password)
        {
            _contentStore = contentStore;
            _password = password;
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
            var currentLogPosition = _contentStore.Repository.FetchLogPosition(logName);

            var salt = AesGcmCrypto.GenerateSalt();
            var iterations = Iterations;
            InitializeKey(salt, iterations);

            if (currentLogPosition.HasValue)
            {
                var currentSnapshotReference = _contentStore.Repository
                    .RetrieveFromLog(logName, currentLogPosition.Value)
                    .ToObject<SnapshotReference>();

                salt = currentSnapshotReference.Salt;
                iterations = currentSnapshotReference.Iterations;
                InitializeKey(salt, iterations);
                var currentSnapshot = ParseSnapshot(currentSnapshotReference);

                foreach (var contentReference in currentSnapshot.ContentReferences)
                {
                    // Known files should be encrypted using the existing
                    // parameters, so we register all previous references
                    _noncesByName[contentReference.Name] = contentReference.Nonce;
                }
            }

            var snapshot = new Snapshot(creationTime, StoreContentItems());
            using var snapshotStream = new MemoryStream(snapshot.ToBytes());
            var snapshotReference = SnapshotReference.FromContentReference(
                _contentStore.StoreContent(snapshotStream,
                    SnapshotContentName,
                    GetNonce(SnapshotContentName),
                    _key),
                salt,
                iterations);

            return _contentStore.Repository.AppendToLog(
                snapshotReference.ToBytes(),
                logName,
                currentLogPosition);
        }

        public void Restore(Uri snapshotUri, Func<string, Stream> writeFunc, string restoreRegex)
        {
            var snapshot = RetrieveSnapshot(snapshotUri);
            var compiledRegex = new Regex(restoreRegex);

            foreach (var contentReference in snapshot.ContentReferences)
            {
                if (compiledRegex.IsMatch(contentReference.Name))
                {
                    _log.Information("Restoring: {File}", contentReference.Name);
                    using var stream = writeFunc(contentReference.Name);
                    _contentStore.RetrieveContent(contentReference, stream, _key);
                }
            }
        }

        public void VerifySnapshot(Uri snapshotUri)
        {
            var snapshot = RetrieveSnapshot(snapshotUri);

            foreach (var contentReference in snapshot.ContentReferences)
            {
                _log.Information("Verifying: {File}", contentReference.Name);

                foreach (var chunk in contentReference.Chunks)
                {
                    _contentStore.Repository.ThrowIfInvalid(chunk.ContentUri);
                }
            }
        }

        public IEnumerable<string> List(Uri snapshotUri, string listRegex)
        {
            var snapshot = RetrieveSnapshot(snapshotUri);
            var compiledRegex = new Regex(listRegex);

            foreach (var contentReferences in snapshot.ContentReferences)
            {
                if (compiledRegex.IsMatch(contentReferences.Name))
                {
                    yield return contentReferences.Name;
                }
            }
        }

        public void Push(string logName, IRepository destinationRepository)
        {
            var sourceLogPosition = _contentStore.Repository.FetchLogPosition(logName);
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
                var source = _contentStore.Repository.RetrieveFromLog(logName, i);
                var destination = destinationRepository.RetrieveFromLog(logName, i);

                if (!Enumerable.SequenceEqual(source, destination))
                {
                    throw new ChunkyardException($"Logs differ at position {i}");
                }
            }

            for (int i = commonLogPosition + 1; i <= sourceLogPosition; i++)
            {
                _log.Information("Pushing snapshot with position: {LogPosition}", i);

                var snapshotReference = _contentStore.Repository.RetrieveFromLog(logName, i)
                    .ToObject<SnapshotReference>();

                InitializeKey(snapshotReference.Salt, snapshotReference.Iterations);
                PushSnapshot(snapshotReference, destinationRepository);
                destinationRepository.AppendToLog(
                    snapshotReference.ToBytes(),
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
            var snapshotReference = _contentStore.Repository
                .RetrieveFromLog(snapshotUri)
                .ToObject<SnapshotReference>();

            InitializeKey(snapshotReference.Salt, snapshotReference.Iterations);
            return ParseSnapshot(snapshotReference);
        }

        private Snapshot ParseSnapshot(ContentReference snapshotReference)
        {
            using var memoryBuffer = new MemoryStream();
            _contentStore.RetrieveContent(snapshotReference, memoryBuffer, _key);

            return memoryBuffer.ToArray().ToObject<Snapshot>();
        }

        private IEnumerable<ContentReference> StoreContentItems()
        {
            foreach (var (readFunc, contentName) in _contentItems)
            {
                _log.Information("Storing: {Content}", contentName);
                using var stream = readFunc();
                yield return _contentStore.StoreContent(stream, contentName, GetNonce(contentName), _key);
            }
        }

        private void PushSnapshot(ContentReference snapshotReference, IRepository remoteRepository)
        {
            var snapshot = ParseSnapshot(snapshotReference);

            foreach (var contentReferences in snapshot.ContentReferences)
            {
                _log.Information("Pushing content: {Content}", contentReferences.Name);

                foreach (var chunk in contentReferences.Chunks)
                {
                    _contentStore.Repository.PushContent(chunk.ContentUri, remoteRepository);
                }
            }

            foreach (var chunk in snapshotReference.Chunks)
            {
                _contentStore.Repository.PushContent(chunk.ContentUri, remoteRepository);
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
    }
}
