using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Chunkyard
{
    /// <summary>
    /// A class which uses <see cref="IContentStore"/> and <see
    /// cref="IRepository"/> to store snapshots of a set of files.
    /// </summary>
    public class SnapshotStore
    {
        private const string SnapshotFile = ".chunkyard-snapshot";

        private readonly IRepository _repository;
        private readonly IContentStore _contentStore;
        private readonly Dictionary<string, ContentReference> _knownContentReferences;

        public SnapshotStore(
            IRepository repository,
            IContentStore contentStore)
        {
            _repository = repository;
            _contentStore = contentStore.EnsureNotNull(nameof(contentStore));
            _knownContentReferences = new Dictionary<string, ContentReference>();
        }

        public int AppendSnapshot(
            IEnumerable<(string Name, Func<Stream> OpenRead)> contents,
            DateTime creationTime)
        {
            RegisterPreviousContent();

            contents.EnsureNotNull(nameof(contents));

            var contentReferences = new List<ContentReference>();

            foreach (var content in contents)
            {
                using var contentStream = content.OpenRead();
                var contentReference = _contentStore.StoreBlob(
                    contentStream,
                    content.Name,
                    GenerateNonce(content.Name),
                    out _);

                contentReferences.Add(contentReference);
            }

            var snapshotContentReference = _contentStore.StoreDocument(
                new Snapshot(
                    Snapshot.SchemaVersion,
                    creationTime,
                    contentReferences),
                SnapshotFile,
                AesGcmCrypto.GenerateNonce(),
                out _);

            var currentLogPosition = _contentStore.CurrentLogPosition;
            var newLogPosition = currentLogPosition.HasValue
                ? currentLogPosition.Value + 1
                : 0;

            var logId = currentLogPosition.HasValue
                ? _contentStore.RetrieveFromLog(currentLogPosition.Value).LogId
                : Guid.NewGuid();

            currentLogPosition = _contentStore.AppendToLog(
                logId,
                newLogPosition,
                snapshotContentReference);

            return currentLogPosition.Value;
        }

        public bool CheckSnapshotExists(
            int logPosition,
            string fuzzyPattern = "")
        {
            var snapshot = GetSnapshot(logPosition);
            var filteredContentReferences = FuzzyFilter(
                snapshot.ContentReferences,
                fuzzyPattern);

            foreach (var contentReference in filteredContentReferences)
            {
                if (!_contentStore.ContentExists(contentReference))
                {
                    return false;
                }
            }

            return true;
        }

        public bool CheckSnapshotValid(
            int logPosition,
            string fuzzyPattern = "")
        {
            var snapshot = GetSnapshot(logPosition);
            var filteredContentReferences = FuzzyFilter(
                snapshot.ContentReferences,
                fuzzyPattern);

            foreach (var contentReference in filteredContentReferences)
            {
                if (!_contentStore.ContentValid(contentReference))
                {
                    return false;
                }
            }

            return true;
        }

        public void RestoreSnapshot(
            int logPosition,
            string fuzzyPattern,
            Func<string, Stream> openWrite)
        {
            var snapshot = GetSnapshot(logPosition);
            var filteredContentReferences = FuzzyFilter(
                snapshot.ContentReferences,
                fuzzyPattern);

            foreach (var contentReference in filteredContentReferences)
            {
                using var stream = openWrite(contentReference.Name);

                _contentStore.RetrieveContent(contentReference, stream);
            }
        }

        public Snapshot GetSnapshot(int logPosition)
        {
            return GetSnapshot(
                _contentStore.RetrieveFromLog(logPosition).ContentReference);
        }

        public IEnumerable<ContentReference> ShowSnapshot(
            int logPosition,
            string fuzzyPattern)
        {
            var snapshot = GetSnapshot(logPosition);
            var filteredContentReferences = FuzzyFilter(
                snapshot.ContentReferences,
                fuzzyPattern);

            foreach (var contentReference in filteredContentReferences)
            {
                yield return contentReference;
            }
        }

        public void GarbageCollect()
        {
            var usedUris = new HashSet<Uri>();
            var allContentUris = _repository.ListUris()
                .ToArray();

            var logPositions = _repository.ListLogPositions();

            foreach (var logPosition in logPositions)
            {
                usedUris.UnionWith(
                    ListUris(logPosition));
            }

            foreach (var contentUri in allContentUris.Except(usedUris))
            {
                _repository.RemoveValue(contentUri);
            }
        }

        public IEnumerable<int> CopySnapshots(
            SnapshotStore otherSnapshotStore)
        {
            otherSnapshotStore.EnsureNotNull(nameof(otherSnapshotStore));

            var thisLogs = _repository
                .ListLogPositions()
                .ToArray();

            var otherContentStore = otherSnapshotStore._contentStore;
            var otherLogs = otherSnapshotStore._repository
                .ListLogPositions()
                .ToArray();

            if (thisLogs.Length > 0 && otherLogs.Length > 0)
            {
                var thisLogReference = _contentStore.RetrieveFromLog(
                    thisLogs[0]);

                var otherLogReference = otherContentStore.RetrieveFromLog(
                    otherLogs[0]);

                if (thisLogReference.LogId != otherLogReference.LogId)
                {
                    throw new ChunkyardException(
                        "Cannot operate on repositories with different log IDs");
                }
            }

            foreach (var logPosition in thisLogs.Intersect(otherLogs))
            {
                var thisLogReference = _contentStore.RetrieveFromLog(
                    logPosition);

                var otherLogReference = otherContentStore.RetrieveFromLog(
                    logPosition);

                if (!thisLogReference.Equals(otherLogReference))
                {
                    throw new ChunkyardException(
                        $"Repositories differ at common snapshot #{logPosition}");
                }
            }

            var otherMax = otherLogs.Length == 0
                ? -1
                : otherLogs.Max();

            var newLogPositions = thisLogs
                .Where(l => l > otherMax)
                .ToArray();

            foreach (var logPosition in newLogPositions)
            {
                CopySnapshot(
                    logPosition,
                    otherSnapshotStore._repository);
            }

            return newLogPositions;
        }

        private void CopySnapshot(
            int logPosition,
            IRepository otherRepository)
        {
            var urisToCopy = ListUris(logPosition).Except(
                otherRepository.ListUris());

            foreach (var contentUri in urisToCopy)
            {
                otherRepository.StoreValue(
                    contentUri,
                    _repository.RetrieveValue(contentUri));
            }

            otherRepository.AppendToLog(
                logPosition,
                _repository.RetrieveFromLog(logPosition));
        }

        public IEnumerable<Uri> ListUris(int logPosition)
        {
            var uris = new List<Uri>();
            var snapshotReference = _contentStore.RetrieveFromLog(logPosition)
                .ContentReference;

            uris.AddRange(_contentStore.ListUris(snapshotReference));

            var snapshot = GetSnapshot(snapshotReference);

            foreach (var contentReference in snapshot.ContentReferences)
            {
                uris.AddRange(_contentStore.ListUris(contentReference));
            }

            return uris;
        }

        private Snapshot GetSnapshot(ContentReference contentReference)
        {
            return _contentStore.RetrieveDocument<Snapshot>(
                contentReference);
        }

        private void RegisterPreviousContent()
        {
            if (_contentStore.CurrentLogPosition == null)
            {
                return;
            }

            var currentSnapshot = GetSnapshot(
                _contentStore.CurrentLogPosition.Value);

            foreach (var contentReference in currentSnapshot.ContentReferences)
            {
                _knownContentReferences[contentReference.Name] =
                    contentReference;
            }
        }

        private byte[] GenerateNonce(string contentName)
        {
            _knownContentReferences.TryGetValue(
                contentName,
                out var knownContentReference);

            // Known files should be encrypted using the same nonce
            return knownContentReference == null
                ? AesGcmCrypto.GenerateNonce()
                : knownContentReference.Nonce;
        }

        private static IEnumerable<ContentReference> FuzzyFilter(
            IEnumerable<ContentReference> contentReferences,
            string fuzzyPattern)
        {
            var fuzzy = new Fuzzy(fuzzyPattern);

            return contentReferences.Where(c => fuzzy.IsMatch(c.Name));
        }
    }
}
