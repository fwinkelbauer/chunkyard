using System;
using System.Collections.Generic;
using System.IO;

namespace Chunkyard
{
    /// <summary>
    /// A class used to create snapshots of the file system. These snapshots are
    /// stored in an <see cref="IContentStore"/>.
    /// </summary>
    public class SnapshotBuilder
    {
        private readonly object _lock;
        private readonly Dictionary<string, ContentReference> _knownContentReferences;
        private readonly List<ContentReference> _storedContentReferences;

        private int? _currentLogPosition;

        public SnapshotBuilder(
            IContentStore contentStore,
            int? currentLogPosition)
        {
            ContentStore = contentStore;

            _currentLogPosition = currentLogPosition;

            _lock = new object();
            _knownContentReferences = new Dictionary<string, ContentReference>();
            _storedContentReferences = new List<ContentReference>();

            if (_currentLogPosition == null)
            {
                return;
            }

            var currentSnapshot = GetSnapshot(_currentLogPosition.Value);

            foreach (var contentReference in currentSnapshot.ContentReferences)
            {
                _knownContentReferences.Add(
                    contentReference.Name,
                    contentReference);
            }
        }

        public IContentStore ContentStore { get; }

        public void AddContent(Stream inputStream, string contentName)
        {
            ContentReference? contentReference = null;
            ContentReference? previousContentReference = null;

            lock (_lock)
            {
                _knownContentReferences.TryGetValue(
                    contentName,
                    out previousContentReference);
            }

            if (previousContentReference == null)
            {
                contentReference = ContentStore.StoreContent(
                    inputStream,
                    contentName);
            }
            else
            {
                contentReference = ContentStore.StoreContent(
                    inputStream,
                    previousContentReference);
            }

            lock (_lock)
            {
                _storedContentReferences.Add(contentReference);
                _knownContentReferences[contentReference.Name] =
                    contentReference;
            }
        }

        public int WriteSnapshot(DateTime creationTime)
        {
            var contentReference = ContentStore.StoreContentObject(
                new Snapshot(creationTime, _storedContentReferences),
                string.Empty);

            _storedContentReferences.Clear();

            _currentLogPosition = ContentStore.AppendToLog(
                contentReference,
                _currentLogPosition);

            return _currentLogPosition.Value;
        }

        public Snapshot GetSnapshot(int logPosition)
        {
            var resolvedLogPosition = Resolve(logPosition);
            var logReference = ContentStore
                .RetrieveFromLog(resolvedLogPosition);

            return GetSnapshot(logReference);
        }

        public void RemoveSnapshot(int logPosition)
        {
            ContentStore.RemoveFromLog(
                Resolve(logPosition));
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

            var snapshot = GetSnapshot(logReference);

            foreach (var contentReference in snapshot.ContentReferences)
            {
                foreach (var chunk in contentReference.Chunks)
                {
                    yield return chunk.ContentUri;
                }
            }
        }

        private Snapshot GetSnapshot(LogReference logReference)
        {
            return ContentStore.RetrieveContentObject<Snapshot>(
                logReference.ContentReference);
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
