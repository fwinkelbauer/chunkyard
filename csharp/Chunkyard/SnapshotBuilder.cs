using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Serilog;

namespace Chunkyard
{
    internal class SnapshotBuilder
    {
        private const string SnapshotContentName = "snapshot";
        private const int Iterations = 1000;

        private static readonly ILogger _log =
            Log.ForContext<SnapshotBuilder>();

        private readonly IContentStore _contentStore;
        private readonly KeyInformation _key;
        private readonly List<ContentReference> _storedContentReferences;

        private int? _currentLogPosition;

        private SnapshotBuilder(
            IContentStore contentStore,
            KeyInformation key,
            int? currentLogPosition)
        {
            _contentStore = contentStore;
            _key = key;
            _currentLogPosition = currentLogPosition;

            _storedContentReferences = new List<ContentReference>();
        }

        public void AddContent(Stream inputStream, string contentName)
        {
            _storedContentReferences.Add(_contentStore.StoreContent(
                inputStream,
                _key.Key,
                contentName));
        }

        public void WriteSnapshot(DateTime creationTime)
        {
            var contentReference = _contentStore.StoreContent(
                new Snapshot(creationTime, _storedContentReferences),
                _key.Key,
                SnapshotContentName);

            // We do not want to leak any fingerprints in an unencrypted
            // reference
            var safeContentReference = new ContentReference(
                contentReference.Name,
                contentReference.Chunks.Select(
                    c => new Chunk(
                        c.ContentUri,
                        string.Empty,
                        c.Nonce,
                        c.Tag)));

            _currentLogPosition = _contentStore.AppendToLog(
                new SnapshotReference(
                    safeContentReference,
                    _key.Salt,
                    _key.Iterations),
                _currentLogPosition);
        }

        public void RestoreSnapshot(
            int restoreLogPosition,
            Func<string, Stream> writeFunc,
            string restoreFuzzy)
        {
            if (!_currentLogPosition.HasValue)
            {
                throw new ChunkyardException(
                    "Cannot restore snapshot from an empty repository");
            }

            var snapshot = LoadSnapshotFromLog(restoreLogPosition);

            var index = 1;
            var filteredContentReferences = FuzzyFilter(
                restoreFuzzy,
                snapshot.ContentReferences)
                .ToArray();

            foreach (var contentReference in filteredContentReferences)
            {
                _log.Information(
                    "Restoring: {File} ({CurrentIndex}/{MaxIndex})",
                    contentReference.Name,
                    index++,
                    filteredContentReferences.Length);

                using var stream = writeFunc(contentReference.Name);
                _contentStore.RetrieveContent(
                    contentReference,
                    _key.Key,
                    stream);
            }
        }

        public void VerifySnapshot(
            int verifyLogPosition,
            string verifyFuzzy,
            bool shallow)
        {
            var snapshot = LoadSnapshotFromLog(verifyLogPosition);

            var index = 1;
            var filteredContentReferences = FuzzyFilter(
                verifyFuzzy,
                snapshot.ContentReferences)
                .ToArray();

            foreach (var contentReference in filteredContentReferences)
            {
                _log.Information(
                    "Verifying: {File} ({CurrentIndex}/{MaxIndex})",
                    contentReference.Name,
                    index++,
                    filteredContentReferences.Length);

                if (shallow && !_contentStore.ContentExists(contentReference))
                {
                    throw new ChunkyardException(
                        $"Missing content: {contentReference.Name}");
                }
                else if (!_contentStore.ContentValid(contentReference))
                {
                    throw new ChunkyardException(
                        $"Corrupted content: {contentReference.Name}");
                }
            }
        }

        private Snapshot LoadSnapshotFromLog(int logPosition)
        {
            var snapshotReference = _contentStore.RetrieveFromLog<SnapshotReference>(
                logPosition);

            return _contentStore.RetrieveContent<Snapshot>(
                snapshotReference.ContentReference,
                _key.Key);
        }

        private static IEnumerable<ContentReference> FuzzyFilter(
            string fuzzyPattern,
            IEnumerable<ContentReference> contentReferences)
        {
            var fuzzy = new Fuzzy(fuzzyPattern);

            foreach (var contentReference in contentReferences)
            {
                if (fuzzy.IsMatch(contentReference.Name))
                {
                    yield return contentReference;
                }
            }
        }

        public static SnapshotBuilder OpenRepository(
            IPrompt prompt,
            NonceGenerator nonceGenerator,
            IContentStore contentStore)
        {
            var currentLogPosition = contentStore.FetchLogPosition();
            KeyInformation? key = null;

            if (currentLogPosition.HasValue)
            {
                var snapshotReference = contentStore
                    .RetrieveFromLog<SnapshotReference>(
                        currentLogPosition.Value);

                key = AesGcmCrypto.PasswordToKey(
                    prompt.ExistingPassword(),
                    snapshotReference.Salt,
                    snapshotReference.Iterations);

                var snapshot = contentStore.RetrieveContent<Snapshot>(
                    snapshotReference.ContentReference,
                    key.Key);

                // Known chunks should be encrypted using the existing
                // parameters, so we register all previous references
                foreach (var contentReference in snapshot.ContentReferences)
                {
                    foreach (var chunk in contentReference.Chunks)
                    {
                        nonceGenerator.Register(chunk.Fingerprint, chunk.Nonce);
                    }
                }
            }
            else
            {
                key = AesGcmCrypto.PasswordToKey(
                    prompt.NewPassword(),
                    AesGcmCrypto.GenerateSalt(),
                    Iterations);
            }

            return new SnapshotBuilder(
                contentStore,
                key,
                currentLogPosition);
        }
    }
}
