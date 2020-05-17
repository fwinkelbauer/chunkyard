using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Chunkyard
{
    internal class SnapshotBuilder
    {
        private readonly IContentStore _contentStore;
        private readonly List<ContentReference> _storedContentReferences;
        private readonly byte[] _salt;
        private readonly int _iterations;

        private int? _currentLogPosition;

        private SnapshotBuilder(
            IContentStore contentStore,
            int? currentLogPosition,
            IEnumerable<byte> salt,
            int iterations)
        {
            _contentStore = contentStore;
            _currentLogPosition = currentLogPosition;
            _salt = salt.ToArray();
            _iterations = iterations;

            _storedContentReferences = new List<ContentReference>();
        }

        public void AddContent(Stream inputStream, string contentName)
        {
            _storedContentReferences.Add(_contentStore.StoreContent(
                inputStream,
                contentName));
        }

        public int WriteSnapshot(DateTime creationTime)
        {
            var contentReference = _contentStore.StoreContent(
                new Snapshot(creationTime, _storedContentReferences),
                string.Empty);

            // We do not want to leak any fingerprints in an unencrypted
            // reference
            var safeContentReference = new ContentReference(
                contentReference.Name,
                contentReference.Chunks.Select(
                    c => new ChunkReference(
                        c.ContentUri,
                        string.Empty,
                        c.Nonce,
                        c.Tag)));

            _currentLogPosition = _contentStore.AppendToLog(
                new SnapshotReference(
                    safeContentReference,
                    _salt,
                    _iterations),
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

            var snapshotReference = _contentStore
                .RetrieveFromLog<SnapshotReference>(resolveLogPosition);

            return _contentStore.RetrieveContent<Snapshot>(
                snapshotReference.ContentReference);
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
                outputStream);
        }

        public static SnapshotBuilder OpenRepository(
            IPrompt prompt,
            EncryptionProvider encryptionProvider,
            IContentStore contentStore)
        {
            var currentLogPosition = contentStore.FetchLogPosition();

            if (currentLogPosition.HasValue)
            {
                var snapshotReference = contentStore
                    .RetrieveFromLog<SnapshotReference>(
                        currentLogPosition.Value);

                encryptionProvider.Salt = snapshotReference.Salt;
                encryptionProvider.Iterations = snapshotReference.Iterations;
                encryptionProvider.Password = prompt.ExistingPassword();

                var snapshot = contentStore.RetrieveContent<Snapshot>(
                    snapshotReference.ContentReference);

                // Known chunks should be encrypted using the existing
                // parameters, so we register all previous references
                foreach (var innerReference in snapshot.ContentReferences)
                {
                    foreach (var chunk in innerReference.Chunks)
                    {
                        encryptionProvider.RegisterNonce(
                            chunk.Fingerprint,
                            chunk.Nonce);
                    }
                }
            }
            else
            {
                encryptionProvider.Password = prompt.NewPassword();
            }

            return new SnapshotBuilder(
                contentStore,
                currentLogPosition,
                encryptionProvider.Salt,
                encryptionProvider.Iterations);
        }
    }
}
