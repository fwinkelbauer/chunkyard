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

        private int? _currentLogPosition;

        private SnapshotBuilder(
            IContentStore contentStore,
            int? currentLogPosition)
        {
            _contentStore = contentStore;
            _currentLogPosition = currentLogPosition;

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

            _currentLogPosition = _contentStore.AppendToLog(
                contentReference,
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

            var logReference = _contentStore
                .RetrieveFromLog(resolveLogPosition);

            return _contentStore.RetrieveContent<Snapshot>(
                logReference.ContentReference);
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
                var logReference = contentStore.RetrieveFromLog(
                    currentLogPosition.Value);

                encryptionProvider.Salt = logReference.Salt;
                encryptionProvider.Iterations = logReference.Iterations;
                encryptionProvider.Password = prompt.ExistingPassword();

                var snapshot = contentStore.RetrieveContent<Snapshot>(
                    logReference.ContentReference);

                // Known files should be encrypted using the existing
                // parameters, so we register all previous references
                foreach (var contentReference in snapshot.ContentReferences)
                {
                    encryptionProvider.RegisterNonce(
                        contentReference.Name,
                        contentReference.Nonce);
                }
            }
            else
            {
                encryptionProvider.Password = prompt.NewPassword();
            }

            return new SnapshotBuilder(
                contentStore,
                currentLogPosition);
        }
    }
}
