using System;
using System.Collections.Generic;
using System.IO;

namespace Chunkyard
{
    internal class SnapshotBuilder
    {
        private readonly IContentStore _contentStore;
        private readonly List<ContentReference> _storedContentReferences;

        private int? _currentLogPosition;

        public SnapshotBuilder(
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

        public IEnumerable<int> ListLogPositions()
        {
            return _contentStore.ListLogPositions();
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

        public IEnumerable<Uri> ListContents(int logPosition)
        {
            var logReference = _contentStore
                .RetrieveFromLog(logPosition);

            foreach (var chunk in logReference.ContentReference.Chunks)
            {
                yield return chunk.ContentUri;
            }

            if (!_contentStore.ContentExists(logReference.ContentReference))
            {
                yield break;
            }

            var snapshot = _contentStore.RetrieveContent<Snapshot>(
                logReference.ContentReference);

            foreach (var contentReference in snapshot.ContentReferences)
            {
                foreach (var chunk in contentReference.Chunks)
                {
                    yield return chunk.ContentUri;
                }
            }
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
    }
}
