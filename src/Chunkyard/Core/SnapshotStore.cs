using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Chunkyard.Core
{
    /// <summary>
    /// A class which uses <see cref="IContentStore"/> and
    /// <see cref="IRepository{int}"/> to store snapshots of a set of files.
    /// </summary>
    public class SnapshotStore
    {
        public const int LatestSnapshotId = -1;

        private readonly IContentStore _contentStore;
        private readonly IRepository<int> _repository;
        private readonly byte[] _salt;
        private readonly int _iterations;
        private readonly byte[] _key;

        private Snapshot? _currentSnapshot;

        public SnapshotStore(
            IContentStore contentStore,
            IRepository<int> repository,
            IPrompt prompt)
        {
            _contentStore = contentStore.EnsureNotNull(nameof(contentStore));
            _repository = repository.EnsureNotNull(nameof(repository));

            prompt.EnsureNotNull(nameof(prompt));

            var snapshotIds = _repository.ListKeys();

            Array.Sort(snapshotIds);

            int? currentSnapshotId = snapshotIds.Length == 0
                ? null
                : snapshotIds[^1];

            if (currentSnapshotId == null)
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
                var snapshotReference = GetSnapshotReference(
                    currentSnapshotId.Value);

                _salt = snapshotReference.Salt;
                _iterations = snapshotReference.Iterations;
                _key = AesGcmCrypto.PasswordToKey(
                    prompt.ExistingPassword(),
                    _salt,
                    _iterations);

                _currentSnapshot = contentStore.RetrieveDocument<Snapshot>(
                    snapshotReference.DocumentReference,
                    _key);
            }
        }

        public Snapshot AppendSnapshot(
            Blob[] blobs,
            Fuzzy scanFuzzy,
            DateTime creationTimeUtc,
            Func<string, Stream> openRead)
        {
            blobs.EnsureNotNull(nameof(blobs));

            var blobReferences = blobs
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

            _currentSnapshot = new Snapshot(
                _currentSnapshot?.SnapshotId + 1 ?? 0,
                creationTimeUtc,
                blobReferences);

            var snapshotReference = new SnapshotReference(
                _contentStore.StoreDocument(
                    _currentSnapshot,
                    _key,
                    AesGcmCrypto.GenerateNonce()),
                _salt,
                _iterations);

            _repository.StoreValue(
                _currentSnapshot.SnapshotId,
                DataConvert.ToBytes(snapshotReference));

            return _currentSnapshot;
        }

        public bool CheckSnapshotExists(
            int snapshotId,
            Fuzzy includeFuzzy)
        {
            return ShowSnapshot(snapshotId, includeFuzzy)
                .AsParallel()
                .Select(br => _contentStore.ContentExists(br))
                .Aggregate(true, (total, next) => total & next);
        }

        public bool CheckSnapshotValid(
            int snapshotId,
            Fuzzy includeFuzzy)
        {
            return ShowSnapshot(snapshotId, includeFuzzy)
                .AsParallel()
                .Select(br => _contentStore.ContentValid(br))
                .Aggregate(true, (total, next) => total & next);
        }

        public Blob[] RestoreSnapshot(
            int snapshotId,
            Fuzzy includeFuzzy,
            Func<string, Stream> openWrite)
        {
            openWrite.EnsureNotNull(nameof(openWrite));

            var blobReferences = ShowSnapshot(
                snapshotId,
                includeFuzzy);

            return blobReferences
                .AsParallel()
                .Select(blobReference =>
                {
                    using var stream = openWrite(blobReference.Name);

                    _contentStore.RetrieveBlob(
                        blobReference,
                        _key,
                        stream);

                    return new Blob(
                        blobReference.Name,
                        blobReference.CreationTimeUtc,
                        blobReference.LastWriteTimeUtc);
                })
                .ToArray();
        }

        public Snapshot GetSnapshot(int snapshotId)
        {
            var resolvedSnapshotId = ResolveSnapshotId(snapshotId);

            try
            {
                var snapshotReference = GetSnapshotReference(
                    resolvedSnapshotId);

                return GetSnapshot(snapshotReference.DocumentReference);
            }
            catch (ChunkyardException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ChunkyardException(
                    $"Snapshot #{resolvedSnapshotId} does not exist",
                    e);
            }
        }

        public Snapshot[] GetSnapshots()
        {
            var snapshotIds = _repository.ListKeys();

            Array.Sort(snapshotIds);

            return snapshotIds
                .Select(GetSnapshot)
                .ToArray();
        }

        public BlobReference[] ShowSnapshot(
            int snapshotId,
            Fuzzy includeFuzzy)
        {
            return GetSnapshot(snapshotId).BlobReferences
                .Where(c => includeFuzzy.IsMatch(c.Name))
                .ToArray();
        }

        public void GarbageCollect()
        {
            var allContentUris = _contentStore.ListContentUris();
            var usedUris = ListUris();
            var unusedUris = allContentUris.Except(usedUris).ToArray();

            foreach (var contentUri in unusedUris)
            {
                _contentStore.RemoveContent(contentUri);
            }
        }

        private Uri[] ListUris()
        {
            IEnumerable<Uri> ListUris(int snapshotId)
            {
                var documentReference = GetSnapshotReference(snapshotId)
                    .DocumentReference;

                foreach (var contentUri in documentReference.ContentUris)
                {
                    yield return contentUri;
                }

                var snapshot = GetSnapshot(documentReference);

                foreach (var blobReference in snapshot.BlobReferences)
                {
                    foreach (var contentUri in blobReference.ContentUris)
                    {
                        yield return contentUri;
                    }
                }
            }

            return _repository.ListKeys()
                .SelectMany(ListUris)
                .Distinct()
                .ToArray();
        }

        private int ResolveSnapshotId(int snapshotId)
        {
            if (_currentSnapshot == null)
            {
                throw new ChunkyardException(
                    "Cannot operate on an empty repository");
            }

            //  0: the first element
            //  1: the second element
            // -1: the last element
            // -2: the second-last element
            return snapshotId >= 0
                ? snapshotId
                : _currentSnapshot.SnapshotId + snapshotId + 1;
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

        private SnapshotReference GetSnapshotReference(int snapshotId)
        {
            return DataConvert.ToObject<SnapshotReference>(
                _repository.RetrieveValue(snapshotId));
        }
    }
}
