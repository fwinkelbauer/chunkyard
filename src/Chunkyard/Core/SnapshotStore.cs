using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    /// <summary>
    /// A class which uses <see cref="IRepository{int}"/> and
    /// <see cref="IRepository{Uri}"/> to store snapshots of a set
    /// of files.
    /// </summary>
    public class SnapshotStore
    {
        public const int LatestSnapshotId = -1;

        private readonly IRepository<Uri> _uriRepository;
        private readonly IRepository<int> _intRepository;
        private readonly FastCdc _fastCdc;
        private readonly string _hashAlgorithmName;
        private readonly IPrompt _prompt;
        private readonly IProbe _probe;
        private readonly byte[] _salt;
        private readonly int _iterations;

        private int? _currentSnapshotId;
        private byte[]? _key;

        public SnapshotStore(
            IRepository<Uri> uriRepository,
            IRepository<int> intRepository,
            FastCdc fastCdc,
            string hashAlgorithmName,
            IPrompt prompt,
            IProbe probe)
        {
            _uriRepository = uriRepository.EnsureNotNull(nameof(uriRepository));
            _intRepository = intRepository.EnsureNotNull(nameof(intRepository));
            _fastCdc = fastCdc;
            _hashAlgorithmName = hashAlgorithmName;
            _prompt = prompt.EnsureNotNull(nameof(prompt));
            _probe = probe.EnsureNotNull(nameof(probe));

            var snapshotIds = _intRepository.ListKeys();

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

        public bool IsEmpty => !_currentSnapshotId.HasValue;

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

        public int StoreSnapshot(
            Blob[] blobs,
            Fuzzy scanFuzzy,
            DateTime creationTimeUtc,
            Func<string, Stream> openRead)
        {
            var newSnapshot = new Snapshot(
                _currentSnapshotId == null
                    ? 0
                    : _currentSnapshotId.Value + 1,
                creationTimeUtc,
                WriteBlobs(blobs, scanFuzzy, openRead));

            var nonce = AesGcmCrypto.GenerateNonce();
            using var memoryStream = new MemoryStream(
                DataConvert.ToBytes(newSnapshot));

            var newSnapshotReference = new SnapshotReference(
                nonce,
                WriteChunks(nonce, memoryStream),
                _salt,
                _iterations);

            _intRepository.StoreValue(
                newSnapshot.SnapshotId,
                DataConvert.ToBytes(newSnapshotReference));

            _currentSnapshotId = newSnapshot.SnapshotId;

            _probe.StoredSnapshot(newSnapshot.SnapshotId);

            return newSnapshot.SnapshotId;
        }

        public bool CheckSnapshotExists(
            int snapshotId,
            Fuzzy includeFuzzy)
        {
            var snapshotExists = ShowSnapshot(snapshotId, includeFuzzy)
                .AsParallel()
                .Select(blobReference =>
                {
                    var blobExists = blobReference.ContentUris
                        .Select(contentUri => _uriRepository.ValueExists(
                            contentUri))
                        .Aggregate(true, (total, next) => total & next);

                    _probe.BlobValid(blobReference, blobExists);

                    return blobExists;
                })
                .Aggregate(true, (total, next) => total & next);

            _probe.SnapshotValid(
                ResolveSnapshotId(snapshotId),
                snapshotExists);

            return snapshotExists;
        }

        public bool CheckSnapshotValid(
            int snapshotId,
            Fuzzy includeFuzzy)
        {
            var snapshotValid = ShowSnapshot(snapshotId, includeFuzzy)
                .AsParallel()
                .Select(blobReference =>
                {
                    var blobValid = blobReference.ContentUris
                        .Select(contentUri =>
                        {
                            return _uriRepository.ValueExists(contentUri)
                                && Id.ContentUriValid(
                                    contentUri,
                                    _uriRepository.RetrieveValue(contentUri));
                        })
                        .Aggregate(true, (total, next) => total & next);

                    _probe.BlobValid(blobReference, blobValid);

                    return blobValid;
                })
                .Aggregate(true, (total, next) => total & next);

            _probe.SnapshotValid(
                ResolveSnapshotId(snapshotId),
                snapshotValid);

            return snapshotValid;
        }

        public Blob[] RetrieveSnapshot(
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

                    RetrieveContent(
                        blobReference.ContentUris,
                        stream);

                    _probe.RetrievedBlob(blobReference);

                    return blobReference.ToBlob();
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

                return GetSnapshot(snapshotReference);
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
            var snapshotIds = _intRepository.ListKeys();

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
                .Where(br => includeFuzzy.IsMatch(br.Name))
                .ToArray();
        }

        public void GarbageCollect()
        {
            var unusedContentUris = _uriRepository.ListKeys()
                .Except(ListUris())
                .ToArray();

            foreach (var contentUri in unusedContentUris)
            {
                _uriRepository.RemoveValue(contentUri);

                _probe.RemovedChunk(contentUri);
            }
        }

        public void RemoveSnapshot(int snapshotId)
        {
            _intRepository.RemoveValue(
                ResolveSnapshotId(snapshotId));

            _probe.RemovedSnapshot(snapshotId);
        }

        public void KeepSnapshots(int latestCount)
        {
            var snapshotIds = _intRepository.ListKeys();

            Array.Sort(snapshotIds);

            var snapshotIdsToKeep = snapshotIds.TakeLast(latestCount);
            var snapshotIdsToDelete = snapshotIds.Except(snapshotIdsToKeep);

            foreach (var snapshotId in snapshotIdsToDelete)
            {
                RemoveSnapshot(snapshotId);
            }
        }

        public void Copy(
            IRepository<Uri> contentRepository,
            IRepository<int> snapshotRepository)
        {
            contentRepository.EnsureNotNull(nameof(contentRepository));
            snapshotRepository.EnsureNotNull(nameof(snapshotRepository));

            var contentUrisToCopy = _uriRepository.ListKeys()
                .Except(contentRepository.ListKeys())
                .ToArray();

            foreach (var contentUri in contentUrisToCopy)
            {
                contentRepository.StoreValue(
                    contentUri,
                    RetrieveValidChunk(contentUri));

                _probe.CopiedChunk(contentUri);
            }

            var snapshotIdsToCopy = _intRepository.ListKeys()
                .Except(snapshotRepository.ListKeys())
                .ToArray();

            foreach (var snapshotId in snapshotIdsToCopy)
            {
                // We convert a SnapshotReference from/to bytes to check if
                // these bytes are valid
                snapshotRepository.StoreValue(
                    snapshotId,
                    DataConvert.ToBytes(
                        GetSnapshotReference(snapshotId)));

                _probe.CopiedSnapshot(snapshotId);
            }
        }

        public void RetrieveContent(
            IEnumerable<Uri> contentUris,
            Stream outputStream)
        {
            contentUris.EnsureNotNull(nameof(contentUris));
            outputStream.EnsureNotNull(nameof(outputStream));

            try
            {
                foreach (var contentUri in contentUris)
                {
                    var decrypted = AesGcmCrypto.Decrypt(
                        _uriRepository.RetrieveValue(contentUri),
                        Key);

                    outputStream.Write(decrypted);
                }
            }
            catch (CryptographicException e)
            {
                throw new ChunkyardException("Could not decrypt data", e);
            }
        }

        private byte[] RetrieveValidChunk(Uri contentUri)
        {
            var value = _uriRepository.RetrieveValue(contentUri);

            if (!Id.ContentUriValid(contentUri, value))
            {
                throw new ChunkyardException(
                    $"Invalid chunk: {contentUri}");
            }

            return value;
        }

        private Uri[] WriteChunks(byte[] nonce, Stream stream)
        {
            return _fastCdc.SplitIntoChunks(stream)
                .Select(chunk =>
                {
                    var encryptedData = AesGcmCrypto.Encrypt(
                        nonce,
                        chunk.Value,
                        Key);

                    var contentUri = Id.ComputeContentUri(
                        _hashAlgorithmName,
                        encryptedData);

                    _uriRepository.StoreValue(contentUri, encryptedData);

                    return contentUri;
                })
                .ToArray();
        }

        private BlobReference[] WriteBlobs(
            Blob[] blobs,
            Fuzzy scanFuzzy,
            Func<string, Stream> openRead)
        {
            // Load Key here so that it does not happen in the parallel loop
            _ = Key;

            var currentBlobReferences = _currentSnapshotId == null
                ? new Dictionary<string, BlobReference>()
                : GetSnapshot(_currentSnapshotId.Value).BlobReferences
                    .ToDictionary(br => br.Name, br => br);

            return blobs
                .AsParallel()
                .Select(blob =>
                {
                    currentBlobReferences.TryGetValue(blob.Name, out var current);

                    if (!scanFuzzy.IsMatch(blob.Name)
                        && current != null
                        && current.ToBlob().Equals(blob))
                    {
                        return current;
                    }

                    // Known blobs should be encrypted using the same nonce
                    var nonce = current?.Nonce
                        ?? AesGcmCrypto.GenerateNonce();

                    using var stream = openRead(blob.Name);

                    var blobReference = new BlobReference(
                        blob.Name,
                        blob.LastWriteTimeUtc,
                        nonce,
                        WriteChunks(nonce, stream));

                    _probe.StoredBlob(blobReference);

                    return blobReference;
                })
                .OrderBy(blobReference => blobReference.Name)
                .ToArray();
        }

        private Uri[] ListUris()
        {
            IEnumerable<Uri> ListUris(int snapshotId)
            {
                var snapshotReference = GetSnapshotReference(snapshotId);

                foreach (var contentUri in snapshotReference.ContentUris)
                {
                    yield return contentUri;
                }

                var snapshot = GetSnapshot(snapshotReference);

                foreach (var blobReference in snapshot.BlobReferences)
                {
                    foreach (var contentUri in blobReference.ContentUris)
                    {
                        yield return contentUri;
                    }
                }
            }

            return _intRepository.ListKeys()
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

        private Snapshot GetSnapshot(SnapshotReference snapshotReference)
        {
            try
            {
                using var memoryStream = new MemoryStream();

                RetrieveContent(
                    snapshotReference.ContentUris,
                    memoryStream);

                return DataConvert.ToObject<Snapshot>(memoryStream.ToArray());
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

        private SnapshotReference GetSnapshotReference(int snapshotId)
        {
            try
            {
                return DataConvert.ToObject<SnapshotReference>(
                    _intRepository.RetrieveValue(snapshotId));
            }
            catch (Exception e)
            {
                throw new ChunkyardException(
                    $"Invalid snapshot reference: #{snapshotId}",
                    e);
            }
        }
    }
}
