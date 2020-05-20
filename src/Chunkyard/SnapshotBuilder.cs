using System;
using System.Collections.Generic;
using System.IO;

namespace Chunkyard
{
    internal class SnapshotBuilder
    {
        private readonly List<ContentReference> _storedContentReferences;

        private int? _currentLogPosition;

        public SnapshotBuilder(
            IContentStore contentStore,
            int? currentLogPosition)
        {
            ContentStore = contentStore;

            _currentLogPosition = currentLogPosition;

            _storedContentReferences = new List<ContentReference>();
        }

        public IContentStore ContentStore { get; }

        public void AddContent(Stream inputStream, string contentName)
        {
            _storedContentReferences.Add(ContentStore.StoreContent(
                inputStream,
                contentName));
        }

        public int WriteSnapshot(DateTime creationTime)
        {
            var contentReference = ContentStore.StoreContent(
                new Snapshot(creationTime, _storedContentReferences),
                string.Empty);

            _currentLogPosition = ContentStore.AppendToLog(
                contentReference,
                _currentLogPosition);

            return _currentLogPosition.Value;
        }

        public Snapshot GetSnapshot(int logPosition)
        {
            var resolveLogPosition = Resolve(logPosition);
            var logReference = ContentStore
                .RetrieveFromLog(resolveLogPosition);

            return ContentStore.RetrieveContent<Snapshot>(
                logReference.ContentReference);
        }

        public IEnumerable<Uri> ListUris(int logPosition)
        {
            var logReference = ContentStore
                .RetrieveFromLog(Resolve(logPosition));

            foreach (var chunk in logReference.ContentReference.Chunks)
            {
                yield return chunk.ContentUri;
            }

            if (!ContentStore.ContentExists(logReference.ContentReference))
            {
                yield break;
            }

            var snapshot = ContentStore.RetrieveContent<Snapshot>(
                logReference.ContentReference);

            foreach (var contentReference in snapshot.ContentReferences)
            {
                foreach (var chunk in contentReference.Chunks)
                {
                    yield return chunk.ContentUri;
                }
            }
        }

        private int Resolve(int logPosition)
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
            return logPosition >= 0
                ? logPosition
                : _currentLogPosition.Value + logPosition + 1;
        }
    }
}
