using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Chunkyard.Core
{
    /// <summary>
    /// A class which uses <see cref="ContentStore"/> and
    /// <see cref="IRepository{int}"/> to store snapshots of a set of files.
    /// </summary>
    public class SnapshotStore
    {
        public const int LatestSnapshotId = -1;

        private readonly ContentStore _contentStore;
        private readonly IRepository<int> _repository;
        private readonly IPrompt _prompt;
        private readonly byte[] _salt;
        private readonly int _iterations;

        private int? _currentSnapshotId;
        private byte[]? _key;

        public SnapshotStore(
            ContentStore contentStore,
            IRepository<int> repository,
            IPrompt prompt)
        {
            _contentStore = contentStore.EnsureNotNull(nameof(contentStore));
            _repository = repository.EnsureNotNull(nameof(repository));
            _prompt = prompt.EnsureNotNull(nameof(prompt));

            var snapshotIds = _repository.ListKeys();

            Array.Sort(snapshotIds);

            _currentSnapshotId = snapshotIds.Length == 0
                ? null
                : snapshotIds[^1];

            _key = null;

            if (_currentSnapshotId == null)
            {
                _salt = AesGcmCrypto.GenerateSalt();
                _iterations = AesGcmCrypto.Iterations;
            }
            else
            {
                var snapshotReference = GetSnapshotReference(
                    _currentSnapshotId.Value);

                _salt = snapshotReference.Salt;
                _iterations = snapshotReference.Iterations;
            }
        }

        private byte[] Key
        {
            get
            {
                if (_key != null)
                {
                    return _key;
                }

                var password = _currentSnapshotId == null
                    ? _prompt.NewPassword()
                    : _prompt.ExistingPassword();

                _key = AesGcmCrypto.PasswordToKey(
                    password,
                    _salt,
                    _iterations);

                return _key;
            }
        }

        public Snapshot AppendSnapshot(
            Blob[] blobs,
            Fuzzy scanFuzzy,
            DateTime creationTimeUtc,
            Func<string, Stream> openRead)
        {
            blobs.EnsureNotNull(nameof(blobs));

            // Load Key here so that it does not happen in the parallel loop
            _ = Key;

            Snapshot? currentSnapshot = null;

            if (_currentSnapshotId != null)
            {
                var currentSnapshotReference = GetSnapshotReference(
                    _currentSnapshotId.Value);

                currentSnapshot = GetSnapshot(
                    currentSnapshotReference.DocumentReference);
            }

            var blobReferences = blobs
                .AsParallel()
                .Select(blob =>
                {
                    var previous = currentSnapshot?.Find(blob.Name);

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
                        Key,
                        nonce,
                        stream);
                })
                .ToArray();

            _currentSnapshotId = _currentSnapshotId == null
                ? 0
                : _currentSnapshotId.Value + 1;

            var newSnapshot = new Snapshot(
                _currentSnapshotId.Value,
                creationTimeUtc,
                blobReferences);

            var newSnapshotReference = new SnapshotReference(
                _contentStore.StoreDocument(
                    newSnapshot,
                    Key,
                    AesGcmCrypto.GenerateNonce()),
                _salt,
                _iterations);

            _repository.StoreValue(
                newSnapshot.SnapshotId,
                DataConvert.ToBytes(newSnapshotReference));

            return newSnapshot;
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

            // Load Key here so that it does not happen in the parallel loop
            _ = Key;

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
                        Key,
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
            var allContentUris = _contentStore.Repository.ListKeys();
            var usedUris = ListUris();
            var unusedUris = allContentUris.Except(usedUris).ToArray();

            foreach (var contentUri in unusedUris)
            {
                _contentStore.Repository.RemoveValue(contentUri);
            }
        }

        public void RemoveSnapshot(int snapshotId)
        {
            _repository.RemoveValue(
                ResolveSnapshotId(snapshotId));
        }

        public void KeepSnapshots(int latestCount)
        {
            _repository.KeepLatestValues(latestCount);
        }

        public void Copy(
            IRepository<Uri> contentRepository,
            IRepository<int> snapshotRepository)
        {
            _contentStore.Repository.Copy(contentRepository);
            _repository.Copy(snapshotRepository);
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
            if (_currentSnapshotId == null)
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
                : _currentSnapshotId.Value + snapshotId + 1;
        }

        private Snapshot GetSnapshot(DocumentReference documentReference)
        {
            try
            {
                return _contentStore.RetrieveDocument<Snapshot>(
                    documentReference,
                    Key);
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
