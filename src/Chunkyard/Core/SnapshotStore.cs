﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Chunkyard.Core
{
    /// <summary>
    /// A class which uses <see cref="IContentStore"/> and <see
    /// cref="IRepository"/> to store snapshots of a set of files.
    /// </summary>
    public class SnapshotStore
    {
        private readonly IContentStore _contentStore;
        private readonly IRepository _repository;
        private readonly byte[] _salt;
        private readonly int _iterations;
        private readonly byte[] _key;

        private int? _currentLogPosition;
        private Snapshot? _currentSnapshot;

        public SnapshotStore(
            IContentStore contentStore,
            IPrompt prompt)
        {
            _contentStore = contentStore.EnsureNotNull(nameof(contentStore));
            _repository = _contentStore.Repository;

            prompt.EnsureNotNull(nameof(prompt));

            var logPositions = _repository.ListLogPositions();
            _currentLogPosition = logPositions.Length == 0
                ? null
                : logPositions[^1];

            if (_currentLogPosition == null)
            {
                _salt = AesGcmCrypto.GenerateSalt();
                _iterations = AesGcmCrypto.Iterations;
                _key = AesGcmCrypto.PasswordToKey(
                    prompt.NewPassword(),
                    _salt,
                    _iterations);

                _currentSnapshot = null;
            }
            else
            {
                var logReference = contentStore.RetrieveFromLog(
                    _currentLogPosition.Value);

                _salt = logReference.Salt;
                _iterations = logReference.Iterations;
                _key = AesGcmCrypto.PasswordToKey(
                    prompt.ExistingPassword(),
                    _salt,
                    _iterations);

                _currentSnapshot = contentStore.RetrieveDocument<Snapshot>(
                    logReference.DocumentReference,
                    _key);
            }
        }

        public int AppendSnapshot(
            IEnumerable<Blob> blobs,
            Fuzzy scanFuzzy,
            DateTime creationTime,
            Func<string, Stream> openRead)
        {
            blobs.EnsureNotNull(nameof(blobs));

            var blobReferences = blobs
                .ToArray()
                .AsParallel()
                .Select(blob =>
                {
                    var previous = _currentSnapshot?.Find(blob.Name);

                    if (!scanFuzzy.IsMatch(blob.Name)
                        && previous != null
                        && previous.CreationTimeUtc.Equals(blob.CreationTimeUtc)
                        && previous.LastWriteTimeUtc.Equals(blob.LastWriteTimeUtc))
                    {
                        return previous;
                    }

                    // Known files should be encrypted using the same nonce
                    var nonce = previous?.Nonce
                        ?? AesGcmCrypto.GenerateNonce();

                    using var stream = openRead(blob.Name);

                    return _contentStore.StoreBlob(
                        blob,
                        _key,
                        nonce,
                        stream);
                })
                .ToArray();

            var snapshot = new Snapshot(
                creationTime,
                blobReferences);

            var newLogPosition = _currentLogPosition + 1
                ?? 0;

            var logReference = new LogReference(
                _contentStore.StoreDocument(
                    snapshot,
                    _key,
                    AesGcmCrypto.GenerateNonce()),
                _salt,
                _iterations);

            _contentStore.AppendToLog(
                newLogPosition,
                logReference);

            _currentLogPosition = newLogPosition;
            _currentSnapshot = snapshot;

            return _currentLogPosition.Value;
        }

        public bool CheckSnapshotExists(
            int logPosition,
            Fuzzy includeFuzzy)
        {
            return ShowSnapshot(logPosition, includeFuzzy)
                .AsParallel()
                .Select(br => _contentStore.ContentExists(br))
                .Aggregate(true, (total, next) => total & next);
        }

        public bool CheckSnapshotValid(
            int logPosition,
            Fuzzy includeFuzzy)
        {
            return ShowSnapshot(logPosition, includeFuzzy)
                .AsParallel()
                .Select(br => _contentStore.ContentValid(br))
                .Aggregate(true, (total, next) => total & next);
        }

        public void RestoreSnapshot(
            int logPosition,
            Fuzzy includeFuzzy,
            Func<string, Stream> openWrite)
        {
            openWrite.EnsureNotNull(nameof(openWrite));

            var blobReferences = ShowSnapshot(
                logPosition,
                includeFuzzy);

            Parallel.ForEach(
                blobReferences,
                blobReference =>
                {
                    using var stream = openWrite(blobReference.Name);

                    _contentStore.RetrieveBlob(
                        blobReference,
                        _key,
                        stream);
                });
        }

        public Snapshot GetSnapshot(int logPosition)
        {
            var resolvedLogPosition = ResolveLogPosition(logPosition);

            try
            {
                var logReference = _contentStore.RetrieveFromLog(
                    resolvedLogPosition);

                return GetSnapshot(logReference.DocumentReference);
            }
            catch (ChunkyardException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ChunkyardException(
                    $"Snapshot #{resolvedLogPosition} does not exist",
                    e);
            }
        }

        public BlobReference[] ShowSnapshot(
            int logPosition,
            Fuzzy includeFuzzy)
        {
            return GetSnapshot(logPosition).BlobReferences
                .Where(c => includeFuzzy.IsMatch(c.Name))
                .ToArray();
        }

        public void GarbageCollect()
        {
            var allContentUris = _repository.ListUris();
            var usedUris = ListUris();
            var unusedUris = allContentUris.Except(usedUris).ToArray();

            foreach (var contentUri in unusedUris)
            {
                _repository.RemoveValue(contentUri);
            }
        }

        public void CopySnapshots(IRepository otherRepository)
        {
            otherRepository.EnsureNotNull(nameof(otherRepository));

            var thisLogs = _repository.ListLogPositions();
            var otherLogs = otherRepository.ListLogPositions();
            var intersection = thisLogs.Intersect(otherLogs)
                .ToArray();

            if (intersection.Length == 0
                && otherLogs.Length > 0)
            {
                throw new ChunkyardException(
                    "Cannot operate on repositories without overlapping log positions");
            }

            foreach (var logPosition in intersection)
            {
                var bytes = _repository.RetrieveFromLog(logPosition);
                var otherBytes = otherRepository.RetrieveFromLog(logPosition);

                if (!bytes.SequenceEqual(otherBytes))
                {
                    throw new ChunkyardException(
                        $"Repositories differ at position #{logPosition}");
                }
            }

            var otherMax = otherLogs.Length == 0
                ? -1
                : otherLogs.Max();

            var logPositionsToCopy = thisLogs
                .Where(l => l > otherMax)
                .ToArray();

            var urisToCopy = ListUris(logPositionsToCopy)
                .Except(otherRepository.ListUris())
                .ToArray();

            foreach (var contentUri in urisToCopy)
            {
                otherRepository.StoreValue(
                    contentUri,
                    _repository.RetrieveValue(contentUri));
            }

            foreach (var logPosition in logPositionsToCopy)
            {
                otherRepository.AppendToLog(
                    logPosition,
                    _repository.RetrieveFromLog(logPosition));
            }
        }

        private Uri[] ListUris()
        {
            return ListUris(
                _repository.ListLogPositions());
        }

        private Uri[] ListUris(IEnumerable<int> logPositions)
        {
            return logPositions
                .SelectMany(ListUris)
                .Distinct()
                .ToArray();
        }

        private Uri[] ListUris(int logPosition)
        {
            var uris = new List<Uri>();
            var documentReference = _contentStore.RetrieveFromLog(logPosition)
                .DocumentReference;

            uris.AddRange(documentReference.ContentUris);

            var snapshot = GetSnapshot(documentReference);

            foreach (var blobReference in snapshot.BlobReferences)
            {
                uris.AddRange(blobReference.ContentUris);
            }

            return uris.ToArray();
        }

        private int ResolveLogPosition(int logPosition)
        {
            if (!_currentLogPosition.HasValue)
            {
                throw new ChunkyardException(
                    "Cannot operate on an empty repository");
            }

            //  0: the first element
            //  1: the second element
            // -1: the last element
            // -2: the second-last element
            return logPosition >= 0
                ? logPosition
                : _currentLogPosition.Value + logPosition + 1;
        }

        private Snapshot GetSnapshot(DocumentReference documentReference)
        {
            try
            {
                return _contentStore.RetrieveDocument<Snapshot>(
                    documentReference,
                    _key);
            }
            catch (Exception e)
            {
                throw new ChunkyardException(
                    "Could not retrieve snapshot",
                    e);
            }
        }
    }
}
