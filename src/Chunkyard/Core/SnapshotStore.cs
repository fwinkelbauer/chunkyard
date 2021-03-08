using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private readonly bool _useCache;
        private readonly byte[] _salt;
        private readonly int _iterations;
        private readonly byte[] _key;

        private int? _currentLogPosition;
        private Snapshot? _currentSnapshot;

        public SnapshotStore(
            IContentStore contentStore,
            IPrompt prompt,
            bool useCache)
        {
            _contentStore = contentStore.EnsureNotNull(nameof(contentStore));
            _repository = _contentStore.Repository;
            _useCache = useCache;

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
            DateTime creationTime)
        {
            blobs.EnsureNotNull(nameof(blobs));

            var blobReferences = blobs
                .AsParallel()
                .Select(blob =>
                {
                    var previous = _currentSnapshot?.BlobReferences
                        .Where(b => b.Name == blob.Name)
                        .FirstOrDefault();

                    if (_useCache
                        && previous != null
                        && previous.CreationTimeUtc.Equals(blob.CreationTimeUtc)
                        && previous.LastWriteTimeUtc.Equals(blob.LastWriteTimeUtc)
                        && _contentStore.ContentExists(previous))
                    {
                        return previous;
                    }

                    // Known files should be encrypted using the same nonce
                    var nonce = previous?.Nonce
                        ?? AesGcmCrypto.GenerateNonce();

                    return _contentStore.StoreBlob(
                        blob,
                        _key,
                        nonce);
                })
                .ToImmutableArray();

            var snapshot = new Snapshot(
                creationTime,
                blobReferences);

            var newLogPosition = _currentLogPosition.HasValue
                ? _currentLogPosition.Value + 1
                : 0;

            var logReference = new LogReference(
                _contentStore.StoreDocument(
                    snapshot,
                    _key,
                    AesGcmCrypto.GenerateNonce()),
                _salt,
                _iterations);

            _currentLogPosition = _contentStore.AppendToLog(
                newLogPosition,
                logReference);

            _currentSnapshot = snapshot;

            return _currentLogPosition.Value;
        }

        public bool CheckSnapshotExists(
            int logPosition,
            string fuzzyPattern = "")
        {
            var blobReferences = ShowSnapshot(
                logPosition,
                fuzzyPattern);

            return blobReferences
                .AsParallel()
                .Select(cr => _contentStore.ContentExists(cr))
                .Aggregate(true, (total, next) => total &= next);
        }

        public bool CheckSnapshotValid(
            int logPosition,
            string fuzzyPattern = "")
        {
            var blobReferences = ShowSnapshot(
                logPosition,
                fuzzyPattern);

            return blobReferences
                .AsParallel()
                .Select(cr => _contentStore.ContentValid(cr))
                .Aggregate(true, (total, next) => total &= next);
        }

        public void RestoreSnapshot(
            int logPosition,
            string fuzzyPattern,
            Func<string, Stream> openWrite)
        {
            openWrite.EnsureNotNull(nameof(openWrite));

            var blobReferences = ShowSnapshot(
                logPosition,
                fuzzyPattern);

            Parallel.ForEach(
                blobReferences,
                blobReference =>
                {
                    using var stream = openWrite(
                        blobReference.Name);

                    _contentStore.RetrieveBlob(
                        blobReference,
                        _key,
                        stream);
                });
        }

        public Snapshot GetSnapshot(int logPosition)
        {
            DocumentReference? documentReference;

            var resolvedLogPosition = ResolveLogPosition(logPosition);

            try
            {
                documentReference = _contentStore.RetrieveFromLog(resolvedLogPosition)
                    .DocumentReference;
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

            return GetSnapshot(documentReference);
        }

        public BlobReference[] ShowSnapshot(
            int logPosition,
            string fuzzyPattern = "")
        {
            var fuzzy = new Fuzzy(fuzzyPattern);

            return GetSnapshot(logPosition).BlobReferences
                .Where(c => fuzzy.IsMatch(c.Name))
                .ToArray();
        }

        public void GarbageCollect()
        {
            var allContentUris = _repository.ListUris();
            var usedUris = ListUris();

            Parallel.ForEach(
                allContentUris.Except(usedUris),
                contentUri => _repository.RemoveValue(contentUri));
        }

        public int[] CopySnapshots(IRepository otherRepository)
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

                if (!bytes.Equals(otherBytes))
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
                .Except(otherRepository.ListUris());

            Parallel.ForEach(
                urisToCopy,
                contentUri =>
                {
                    otherRepository.StoreValue(
                        contentUri,
                        _repository.RetrieveValue(contentUri));
                });

            foreach (var logPosition in logPositionsToCopy)
            {
                otherRepository.AppendToLog(
                    logPosition,
                    _repository.RetrieveFromLog(logPosition));
            }

            return logPositionsToCopy;
        }

        private Uri[] ListUris()
        {
            return ListUris(
                _repository.ListLogPositions());
        }

        private Uri[] ListUris(IEnumerable<int> logPositions)
        {
            return logPositions
                .SelectMany(position => ListUris(position))
                .Distinct()
                .ToArray();
        }

        private Uri[] ListUris(int logPosition)
        {
            var uris = new List<Uri>();
            var snapshotReference = _contentStore.RetrieveFromLog(logPosition)
                .DocumentReference;

            uris.AddRange(
                snapshotReference.Chunks.Select(c => c.ContentUri));

            var snapshot = GetSnapshot(snapshotReference);

            foreach (var blobReference in snapshot.BlobReferences)
            {
                uris.AddRange(
                    blobReference.Chunks.Select(c => c.ContentUri));
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
            catch (ChunkyardException)
            {
                throw;
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
