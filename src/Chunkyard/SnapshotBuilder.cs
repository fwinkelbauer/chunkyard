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
        private readonly IContentStore _contentStore;
        private readonly object _lock;
        private readonly Dictionary<string, ContentReference> _knownContentReferences;
        private readonly List<ContentReference> _storedContentReferences;

        private int? _currentLogPosition;

        public SnapshotBuilder(
            IContentStore contentStore,
            int? currentLogPosition)
        {
            _contentStore = contentStore;

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

        public void AddContent(Stream inputStream, string contentName)
        {
            ContentReference? contentReference;
            ContentReference? previousContentReference;

            lock (_lock)
            {
                _knownContentReferences.TryGetValue(
                    contentName,
                    out previousContentReference);
            }

            if (previousContentReference == null)
            {
                contentReference = _contentStore.StoreContent(
                    inputStream,
                    contentName);
            }
            else
            {
                contentReference = _contentStore.StoreContent(
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
            var contentReference = _contentStore.StoreContentObject(
                new Snapshot(creationTime, _storedContentReferences),
                string.Empty);

            _storedContentReferences.Clear();

            var newLogPosition = _currentLogPosition.HasValue
                ? _currentLogPosition.Value + 1
                : 0;

            _currentLogPosition = _contentStore.AppendToLog(
                contentReference,
                newLogPosition);

            return _currentLogPosition.Value;
        }

        public Snapshot GetSnapshot(int logPosition)
        {
            return GetSnapshot(
                _contentStore.RetrieveFromLog(logPosition));
        }

        public IEnumerable<Uri> ListUris(int logPosition)
        {
            var logReference = _contentStore.RetrieveFromLog(logPosition);

            foreach (var chunk in logReference.ContentReference.Chunks)
            {
                yield return chunk.ContentUri;
            }

            if (!_contentStore.ContentExists(logReference.ContentReference))
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
            return _contentStore.RetrieveContentObject<Snapshot>(
                logReference.ContentReference);
        }

        public int ResolveLogPosition(int logPosition)
        {
            if (!_currentLogPosition.HasValue)
            {
                throw new ChunkyardException(
                    "Cannot load snapshot from an empty repository");
            }

            //  0: the first element
            //  1: the second element
            // -1: the last element
            // -2: the second-last element
            return logPosition >= 0
                ? logPosition
                : _currentLogPosition.Value + logPosition + 1;
        }
    }
}
