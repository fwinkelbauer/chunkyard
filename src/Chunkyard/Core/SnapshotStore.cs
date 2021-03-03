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
        private const string SnapshotFile = ".chunkyard-snapshot";

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
                    logReference.ContentReference,
                    _key);
            }
        }

        public int AppendSnapshot(
            IEnumerable<string> contentNames,
            Func<string, Stream> openRead,
            DateTime creationTime)
        {
            contentNames.EnsureNotNull(nameof(contentNames));
            openRead.EnsureNotNull(nameof(openRead));

            var contentReferences = contentNames
                .Distinct()
                .AsParallel()
                .Select(contentName =>
                {
                    using var contentStream = openRead(contentName);

                    return _contentStore.StoreBlob(
                        contentStream,
                        contentName,
                        _key,
                        GenerateNonce(contentName),
                        out _);
                })
                .ToImmutableArray();

            var snapshot = new Snapshot(
                creationTime,
                contentReferences);

            var logReference = new LogReference(
                _contentStore.StoreDocument(
                    snapshot,
                    SnapshotFile,
                    _key,
                    AesGcmCrypto.GenerateNonce(),
                    out _),
                _salt,
                _iterations);

            var newLogPosition = _currentLogPosition.HasValue
                ? _currentLogPosition.Value + 1
                : 0;

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
            var filteredContentReferences = ShowSnapshot(
                logPosition,
                fuzzyPattern);

            return filteredContentReferences
                .AsParallel()
                .Select(cr => _contentStore.ContentExists(cr))
                .Aggregate(true, (total, next) => total &= next);
        }

        public bool CheckSnapshotValid(
            int logPosition,
            string fuzzyPattern = "")
        {
            var filteredContentReferences = ShowSnapshot(
                logPosition,
                fuzzyPattern);

            return filteredContentReferences
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

            var filteredContentReferences = ShowSnapshot(
                logPosition,
                fuzzyPattern);

            Parallel.ForEach(
                filteredContentReferences,
                contentReference =>
                {
                    using var stream = openWrite(
                        contentReference.Name);

                    _contentStore.RetrieveContent(
                        contentReference,
                        _key,
                        stream);
                });
        }

        public Snapshot GetSnapshot(int logPosition)
        {
            ContentReference? contentReference;

            var resolvedLogPosition = ResolveLogPosition(logPosition);

            try
            {
                contentReference = _contentStore.RetrieveFromLog(resolvedLogPosition)
                    .ContentReference;
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

            return GetSnapshot(contentReference);
        }

        public ContentReference[] ShowSnapshot(
            int logPosition,
            string fuzzyPattern = "")
        {
            var fuzzy = new Fuzzy(fuzzyPattern);
            var contentReferences = GetSnapshot(logPosition).ContentReferences;

            return contentReferences.Where(c => fuzzy.IsMatch(c.Name))
                .Distinct()
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

        private Snapshot GetSnapshot(ContentReference contentReference)
        {
            try
            {
                return _contentStore.RetrieveDocument<Snapshot>(
                    contentReference,
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

        private byte[] GenerateNonce(string contentName)
        {
            // Known files should be encrypted using the same nonce
            var previousNonce = _currentSnapshot?.ContentReferences
                .Where(c => c.Name == contentName)
                .Select(c => c.Nonce)
                .FirstOrDefault();

            return previousNonce ?? AesGcmCrypto.GenerateNonce();
        }
    }
}
