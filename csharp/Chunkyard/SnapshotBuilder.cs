using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Chunkyard
{
    internal class SnapshotBuilder
    {
        private const int Iterations = 1000;

        private readonly List<ContentReference> _storedContentReferences;

        private int? _currentLogPosition;

        private SnapshotBuilder(
            IContentStore contentStore,
            KeyInformation key,
            int? currentLogPosition)
        {
            ContentStore = contentStore;
            Key = key;

            _currentLogPosition = currentLogPosition;

            _storedContentReferences = new List<ContentReference>();
        }

        public IContentStore ContentStore { get; }

        public KeyInformation Key { get; }

        public void AddContent(Stream inputStream, string contentName)
        {
            _storedContentReferences.Add(ContentStore.StoreContent(
                inputStream,
                Key.Key,
                contentName));
        }

        public int WriteSnapshot(DateTime creationTime)
        {
            var contentReference = ContentStore.StoreContent(
                new Snapshot(creationTime, _storedContentReferences),
                Key.Key,
                string.Empty);

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

            _currentLogPosition = ContentStore.AppendToLog(
                new SnapshotReference(
                    safeContentReference,
                    Key.Salt,
                    Key.Iterations),
                _currentLogPosition);

            return _currentLogPosition.Value;
        }

        public IEnumerable<(int, Snapshot)> GetSnapshots()
        {
            foreach (var logPosition in ContentStore.ListLogPositions())
            {
                yield return (logPosition, GetSnapshot(logPosition));
            }
        }

        public Snapshot GetSnapshot(int logPosition)
        {
            if (!_currentLogPosition.HasValue)
            {
                throw new ChunkyardException(
                    "Cannot load snapshot from an empty repository");
            }

            //  0: the first element
            //  1: the second element
            // -2: the second-last element
            // -1: the last element
            var resolveLogPosition = logPosition >= 0
                ? logPosition
                : _currentLogPosition.Value + logPosition + 1;

            var snapshotReference = ContentStore.RetrieveFromLog<SnapshotReference>(
                resolveLogPosition);

            return ContentStore.RetrieveContent<Snapshot>(
                snapshotReference.ContentReference,
                Key.Key);
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
