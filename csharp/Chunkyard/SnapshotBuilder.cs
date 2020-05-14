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

        public int WriteSnapshot(DateTime creationTime)
        {
            var contentReference = _contentStore.StoreContent(
                new Snapshot(creationTime, _storedContentReferences),
                _key.Key,
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

            _currentLogPosition = _contentStore.AppendToLog(
                new SnapshotReference(
                    safeContentReference,
                    _key.Salt,
                    _key.Iterations),
                _currentLogPosition);

            return _currentLogPosition.Value;
        }

        public IEnumerable<(int, Snapshot)> GetSnapshots()
        {
            foreach (var logPosition in _contentStore.ListLogPositions())
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

            var snapshotReference = _contentStore.RetrieveFromLog<SnapshotReference>(
                resolveLogPosition);

            return _contentStore.RetrieveContent<Snapshot>(
                snapshotReference.ContentReference,
                _key.Key);
        }

        public bool ContentExists(ContentReference contentReference)
        {
            return _contentStore.ContentExists(contentReference);
        }

        public bool ContentValid(ContentReference contentReference)
        {
            return _contentStore.ContentValid(contentReference);
        }

        public void RetrieveContent(
            ContentReference contentReference,
            Stream outputStream)
        {
            _contentStore.RetrieveContent(
                contentReference,
                _key.Key,
                outputStream);
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
