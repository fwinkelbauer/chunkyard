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

            var snapshotReference = _contentStore
                .RetrieveFromLog<SnapshotReference>(
                    restoreLogPosition);

            var snapshot = _contentStore.RetrieveContent<Snapshot>(
                snapshotReference.ContentReference,
                _key.Key);

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

/*      public void VerifySnapshot(
            Uri snapshotUri,
            string verifyFuzzy,
            bool shallow)
        {
            var (snapshot, _) = RetrieveSnapshot(
                snapshotUri,
                _prompt.ExistingPassword());

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

                foreach (var chunk in contentReference.Chunks)
                {
                    if (shallow)
                    {
                        _contentStore.Repository.ThrowIfNotExists(
                            chunk.ContentUri);
                    }
                    else
                    {
                        _contentStore.Repository.ThrowIfInvalid(
                            chunk.ContentUri);
                    }
                }
            }
        }

        public Snapshot GetSnapshot(Uri snapshotUri)
        {
            var (snapshot, _) = RetrieveSnapshot(
                snapshotUri,
                _prompt.ExistingPassword());

            return snapshot;
        }

        private Snapshot RetrieveSnapshot(Uri snapshotUri)
        {
            var snapshotReference = _contentStore.Repository
                .RetrieveFromLog(snapshotUri)
                .ToObject<SnapshotReference>();

            return ParseSnapshot(snapshotReference);
        }

        private Snapshot ParseSnapshot(SnapshotReference snapshotReference)
        {
            using var memoryBuffer = new MemoryStream();
            _contentStore.RetrieveContent(snapshotReference.ContentReference, memoryBuffer, BuildKey());

            return memoryBuffer.ToArray().ToObject<Snapshot>();
        }*/
    }
}
