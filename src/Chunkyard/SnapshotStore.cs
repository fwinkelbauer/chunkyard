﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Chunkyard
{
    /// <summary>
    /// A class which uses <see cref="IContentStore"/> and <see
    /// cref="IRepository"/> to store snapshots of a set of files.
    /// </summary>
    public class SnapshotStore
    {
        private const string SnapshotFile = ".chunkyard-snapshot";

        private readonly IContentStore _contentStore;
        private readonly IRepository _repository;
        private readonly Dictionary<string, ContentReference> _knownContentReferences;

        public SnapshotStore(IContentStore contentStore)
        {
            _contentStore = contentStore.EnsureNotNull(nameof(contentStore));
            _repository = contentStore.Repository;
            _knownContentReferences = new Dictionary<string, ContentReference>();
        }

        public int AppendSnapshot(
            IEnumerable<string> contentNames,
            Func<string, Stream> openRead,
            DateTime creationTime)
        {
            RegisterPreviousContent();

            contentNames.EnsureNotNull(nameof(contentNames));
            openRead.EnsureNotNull(nameof(openRead));

            var contentReferences = ImmutableArray.CreateBuilder<ContentReference>();

            foreach (var contentName in contentNames)
            {
                using var contentStream = openRead(contentName);
                var contentReference = _contentStore.StoreBlob(
                    contentStream,
                    contentName,
                    GenerateNonce(contentName),
                    out _);

                contentReferences.Add(contentReference);
            }

            var snapshotContentReference = _contentStore.StoreDocument(
                new Snapshot(
                    creationTime,
                    contentReferences.ToImmutable()),
                SnapshotFile,
                AesGcmCrypto.GenerateNonce(),
                out _);

            var newLogPosition = _contentStore.CurrentLogPosition.HasValue
                ? _contentStore.CurrentLogPosition.Value + 1
                : 0;

            return _contentStore.AppendToLog(
                newLogPosition,
                snapshotContentReference);
        }

        public bool CheckSnapshotExists(
            int logPosition,
            string fuzzyPattern = "")
        {
            var snapshot = GetSnapshot(logPosition);
            var filteredContentReferences = FuzzyFilter(
                snapshot.ContentReferences,
                fuzzyPattern);

            var exists = true;

            foreach (var contentReference in filteredContentReferences)
            {
                exists &= _contentStore.ContentExists(contentReference);
            }

            return exists;
        }

        public bool CheckSnapshotValid(
            int logPosition,
            string fuzzyPattern = "")
        {
            var snapshot = GetSnapshot(logPosition);
            var filteredContentReferences = FuzzyFilter(
                snapshot.ContentReferences,
                fuzzyPattern);

            var valid = true;

            foreach (var contentReference in filteredContentReferences)
            {
                valid &= _contentStore.ContentValid(contentReference);
            }

            return valid;
        }

        public void RestoreSnapshot(
            int logPosition,
            string fuzzyPattern,
            Func<string, Stream> openWrite)
        {
            openWrite.EnsureNotNull(nameof(openWrite));

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

        public ContentReference[] ShowSnapshot(
            int logPosition,
            string fuzzyPattern = "")
        {
            return FuzzyFilter(
                GetSnapshot(logPosition).ContentReferences,
                fuzzyPattern);
        }

        public void GarbageCollect()
        {
            var usedUris = new HashSet<Uri>();
            var allContentUris = _repository.ListUris();
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

        public int[] CopySnapshots(IRepository otherRepository)
        {
            otherRepository.EnsureNotNull(nameof(otherRepository));

            if (!_repository.RepositoryId.Equals(otherRepository.RepositoryId))
            {
                throw new ChunkyardException(
                    "Cannot operate on repositories with different IDs");
            }

            var thisLogs = _repository.ListLogPositions();
            var otherLogs = otherRepository.ListLogPositions();

            foreach (var logPosition in thisLogs.Intersect(otherLogs))
            {
                var bytes = _repository.RetrieveFromLog(logPosition);
                var otherBytes = otherRepository.RetrieveFromLog(logPosition);

                if (!bytes.Equals(otherBytes))
                {
                    throw new ChunkyardException(
                        $"Repositories differ at position #{logPosition}");
                }
            }

            var otherMax = otherLogs.Length == 0
                ? -1
                : otherLogs.Max();

            var newLogPositions = thisLogs
                .Where(l => l > otherMax)
                .ToArray();

            var otherUris = otherRepository.ListUris()
                .ToList();

            foreach (var logPosition in newLogPositions)
            {
                otherUris.AddRange(
                    CopySnapshotUris(
                        logPosition,
                        otherRepository,
                        otherUris));
            }

            return newLogPositions;
        }

        private IEnumerable<Uri> CopySnapshotUris(
            int logPosition,
            IRepository otherRepository,
            IEnumerable<Uri> otherUris)
        {
            var urisToCopy = ListUris(logPosition).Except(otherUris);
            var copiedUris = new List<Uri>();

            foreach (var contentUri in urisToCopy)
            {
                otherRepository.StoreValue(
                    contentUri,
                    _repository.RetrieveValue(contentUri));

                copiedUris.Add(contentUri);
            }

            otherRepository.AppendToLog(
                logPosition,
                _repository.RetrieveFromLog(logPosition));

            return copiedUris;
        }

        private Uri[] ListUris(int logPosition)
        {
            var uris = new List<Uri>();
            var snapshotReference = _contentStore.RetrieveFromLog(logPosition)
                .ContentReference;

            uris.AddRange(
                snapshotReference.Chunks.Select(c => c.ContentUri));

            var snapshot = GetSnapshot(snapshotReference);

            foreach (var contentReference in snapshot.ContentReferences)
            {
                uris.AddRange(
                    contentReference.Chunks.Select(c => c.ContentUri));
            }

            return uris.ToArray();
        }

        private Snapshot GetSnapshot(ContentReference contentReference)
        {
            try
            {
                return _contentStore.RetrieveDocument<Snapshot>(
                    contentReference);
            }
            catch (Exception e)
            {
                throw new ChunkyardException(
                    "Could not retrieve snapshot",
                    e);
            }
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

        private static ContentReference[] FuzzyFilter(
            IEnumerable<ContentReference> contentReferences,
            string fuzzyPattern)
        {
            var fuzzy = new Fuzzy(fuzzyPattern);

            return contentReferences.Where(c => fuzzy.IsMatch(c.Name))
                .ToArray();
        }
    }
}
