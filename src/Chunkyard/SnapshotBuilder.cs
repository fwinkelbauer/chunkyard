﻿using System;
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
        private readonly Dictionary<string, ContentReference> _knownContentReferences;
        private readonly List<ContentReference> _storedContentReferences;

        public SnapshotBuilder(IContentStore contentStore)
        {
            _contentStore = contentStore.EnsureNotNull(nameof(contentStore));

            _knownContentReferences = new Dictionary<string, ContentReference>();
            _storedContentReferences = new List<ContentReference>();

            if (_contentStore.CurrentLogPosition == null)
            {
                return;
            }

            var currentSnapshot = GetSnapshot(
                _contentStore.CurrentLogPosition.Value);

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

            _knownContentReferences.TryGetValue(
                contentName,
                out var previousContentReference);

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

            _storedContentReferences.Add(contentReference);
            _knownContentReferences[contentReference.Name] =
                contentReference;
        }

        public int WriteSnapshot(DateTime creationTime)
        {
            var contentReference = _contentStore.StoreContentObject(
                new Snapshot(creationTime, _storedContentReferences),
                string.Empty);

            _storedContentReferences.Clear();

            var currentLogPosition = _contentStore.CurrentLogPosition;
            var newLogPosition = currentLogPosition.HasValue
                ? currentLogPosition.Value + 1
                : 0;

            var logId = currentLogPosition.HasValue
                ? _contentStore.RetrieveFromLog(currentLogPosition.Value).LogId
                : Guid.NewGuid();

            currentLogPosition = _contentStore.AppendToLog(
                logId,
                contentReference,
                newLogPosition);

            return currentLogPosition.Value;
        }

        public Snapshot GetSnapshot(int logPosition)
        {
            return GetSnapshot(
                _contentStore.RetrieveFromLog(logPosition));
        }

        public IEnumerable<Uri> ListUris(int logPosition)
        {
            var logReference = _contentStore.RetrieveFromLog(logPosition);

            if (!_contentStore.ContentExists(logReference.ContentReference))
            {
                yield break;
            }

            foreach (var chunk in logReference.ContentReference.Chunks)
            {
                yield return chunk.ContentUri;
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
    }
}
