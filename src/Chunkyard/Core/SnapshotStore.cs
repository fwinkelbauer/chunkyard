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
        public const int SecondLatestSnapshotId = -2;

        private readonly IRepository<Uri> _uriRepository;
        private readonly IRepository<int> _intRepository;
        private readonly FastCdc _fastCdc;
        private readonly IProbe _probe;
        private readonly int _parallelizeChunkThreshold;
        private readonly byte[] _salt;
        private readonly int _iterations;
        private readonly Lazy<byte[]> _key;

        private int? _currentSnapshotId;

        public SnapshotStore(
            IRepository<Uri> uriRepository,
            IRepository<int> intRepository,
            FastCdc fastCdc,
            IPrompt prompt,
            IProbe probe,
            int parallelizeChunkThreshold)
        {
            _uriRepository = uriRepository;
            _intRepository = intRepository;
            _fastCdc = fastCdc;
            _probe = probe;
            _parallelizeChunkThreshold = parallelizeChunkThreshold;

            if (parallelizeChunkThreshold <= 0)
            {
                throw new ArgumentException(
                    "Value must be larger than zero",
                    nameof(parallelizeChunkThreshold));
            }

            _currentSnapshotId = FetchCurrentSnapshotId();

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

            _key = new Lazy<byte[]>(() =>
            {
                var password = _currentSnapshotId == null
                    ? prompt.NewPassword()
                    : prompt.ExistingPassword();

                return AesGcmCrypto.PasswordToKey(password, _salt, _iterations);
            });
        }

        public bool IsEmpty => _currentSnapshotId == null;

        public int StoreSnapshot(
            IBlobReader blobReader,
            DateTime creationTimeUtc)
        {
            blobReader.EnsureNotNull(nameof(blobReader));

            var newSnapshot = new Snapshot(
                _currentSnapshotId == null
                    ? 0
                    : _currentSnapshotId.Value + 1,
                creationTimeUtc,
                WriteBlobs(blobReader));

            using var memoryStream = new MemoryStream(
                DataConvert.ObjectToBytes(newSnapshot));

            var newSnapshotReference = new SnapshotReference(
                _salt,
                _iterations,
                WriteContent(AesGcmCrypto.GenerateNonce(), memoryStream));

            _intRepository.StoreValue(
                newSnapshot.SnapshotId,
                DataConvert.ObjectToBytes(newSnapshotReference));

            _currentSnapshotId = newSnapshot.SnapshotId;

            _probe.StoredSnapshot(newSnapshot.SnapshotId);

            return newSnapshot.SnapshotId;
        }

        public bool CheckSnapshotExists(
            int snapshotId,
            Fuzzy includeFuzzy)
        {
            var snapshot = GetSnapshot(snapshotId);
            var snapshotExists = Filter(snapshot, includeFuzzy)
                .AsParallel()
                .Select(blobReference =>
                {
                    var blobExists = blobReference.ContentUris
                        .Select(_uriRepository.ValueExists)
                        .Aggregate(true, (total, next) => total & next);

                    _probe.BlobValid(blobReference, blobExists);

                    return blobExists;
                })
                .Aggregate(true, (total, next) => total & next);

            _probe.SnapshotValid(
                snapshot.SnapshotId,
                snapshotExists);

            return snapshotExists;
        }

        public bool CheckSnapshotValid(
            int snapshotId,
            Fuzzy includeFuzzy)
        {
            var snapshot = GetSnapshot(snapshotId);
            var snapshotValid = Filter(snapshot, includeFuzzy)
                .AsParallel()
                .Select(blobReference =>
                {
                    var blobValid = blobReference.ContentUris
                        .Select(contentUri =>
                        {
                            try
                            {
                                return Id.ContentUriValid(
                                    contentUri,
                                    _uriRepository.RetrieveValue(contentUri));
                            }
                            catch (Exception)
                            {
                                return false;
                            }
                        })
                        .Aggregate(true, (total, next) => total & next);

                    _probe.BlobValid(blobReference, blobValid);

                    return blobValid;
                })
                .Aggregate(true, (total, next) => total & next);

            _probe.SnapshotValid(
                snapshot.SnapshotId,
                snapshotValid);

            return snapshotValid;
        }

        public IEnumerable<Blob> RetrieveSnapshot(
            IBlobWriter blobWriter,
            int snapshotId,
            Fuzzy includeFuzzy)
        {
            var snapshot = GetSnapshot(snapshotId);
            var blobs = Filter(snapshot, includeFuzzy)
                .AsParallel()
                .Select(blobReference =>
                {
                    var existingBlob = blobWriter.FindBlob(blobReference.Name);
                    var snapshotBlob = blobReference.ToBlob();

                    if (existingBlob != null
                        && existingBlob.Equals(snapshotBlob))
                    {
                        return existingBlob;
                    }

                    try
                    {
                        using (var stream = blobWriter.OpenWrite(
                            blobReference.Name))
                        {
                            RetrieveContent(
                                blobReference.ContentUris,
                                stream);
                        }

                        // We want to call this method after disposing the
                        // stream, which is why we are using a dedicated using
                        // block above
                        blobWriter.UpdateBlobMetadata(snapshotBlob);
                    }
                    catch (Exception e)
                    {
                        throw new ChunkyardException(
                            $"Could not restore file {blobReference.Name}",
                            e);
                    }

                    _probe.RetrievedBlob(blobReference);

                    return snapshotBlob;
                })
                .ToArray();

            _probe.RetrievedSnapshot(snapshot.SnapshotId);

            return blobs;
        }

        public Snapshot GetSnapshot(int snapshotId)
        {
            var resolvedSnapshotId = ResolveSnapshotId(snapshotId);

            return GetSnapshot(
                resolvedSnapshotId,
                GetSnapshotReference(resolvedSnapshotId));
        }

        public IEnumerable<Snapshot> GetSnapshots()
        {
            return _intRepository.ListKeys()
                .OrderBy(i => i)
                .Select(GetSnapshot)
                .ToArray();
        }

        public IEnumerable<BlobReference> ShowSnapshot(
            int snapshotId,
            Fuzzy includeFuzzy)
        {
            return Filter(GetSnapshot(snapshotId), includeFuzzy);
        }

        public void GarbageCollect()
        {
            var usedContentUris = FetchContentUris(
                _intRepository.ListKeys());

            var unusedContentUris = _uriRepository.ListKeys()
                .Except(usedContentUris)
                .ToArray();

            foreach (var contentUri in unusedContentUris)
            {
                _uriRepository.RemoveValue(contentUri);

                _probe.RemovedContent(contentUri);
            }
        }

        public void RemoveSnapshot(int snapshotId)
        {
            var resolvedSnapshotId = ResolveSnapshotId(snapshotId);

            _intRepository.RemoveValue(resolvedSnapshotId);
            _probe.RemovedSnapshot(resolvedSnapshotId);

            if (_currentSnapshotId == resolvedSnapshotId)
            {
                _currentSnapshotId = FetchCurrentSnapshotId();
            }
        }

        public void KeepSnapshots(int latestCount)
        {
            var snapshotIds = _intRepository.ListKeys()
                .OrderBy(i => i)
                .ToArray();

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

            Copy(
                contentRepository,
                snapshotRepository,
                mirror: false);
        }

        public void Mirror(
            IRepository<Uri> contentRepository,
            IRepository<int> snapshotRepository)
        {
            contentRepository.EnsureNotNull(nameof(contentRepository));
            snapshotRepository.EnsureNotNull(nameof(snapshotRepository));

            Copy(
                contentRepository,
                snapshotRepository,
                mirror: true);
        }

        private void Copy(
            IRepository<Uri> contentRepository,
            IRepository<int> snapshotRepository,
            bool mirror)
        {
            var localContentUris = _uriRepository.ListKeys();
            var remoteContentUris = contentRepository.ListKeys();

            var contentUrisToCopy = localContentUris
                .Except(remoteContentUris)
                .ToArray();

            foreach (var contentUri in contentUrisToCopy)
            {
                var content = _uriRepository.RetrieveValue(contentUri);

                if (!Id.ContentUriValid(contentUri, content))
                {
                    throw new ChunkyardException(
                        $"Invalid content: {contentUri}");
                }

                contentRepository.StoreValue(contentUri, content);

                _probe.CopiedContent(contentUri);
            }

            var localSnapshotIds = _intRepository.ListKeys();
            var remoteSnapshotIds = snapshotRepository.ListKeys();

            var snapshotIdsToCopy = localSnapshotIds
                .Except(remoteSnapshotIds)
                .ToArray();

            foreach (var snapshotId in snapshotIdsToCopy)
            {
                // We convert a SnapshotReference from/to bytes to check if
                // these bytes are valid
                snapshotRepository.StoreValue(
                    snapshotId,
                    DataConvert.ObjectToBytes(
                        GetSnapshotReference(snapshotId)));

                _probe.CopiedSnapshot(snapshotId);
            }

            if (mirror)
            {
                // We need to delete snapshotIds prior to deleting contentUris
                // so that a cancelled mirror operation does not corrupt any
                // snapshots
                var snapshotIdsToDelete = remoteSnapshotIds
                    .Except(localSnapshotIds)
                    .ToArray();

                foreach (var snapshotId in snapshotIdsToDelete)
                {
                    snapshotRepository.RemoveValue(snapshotId);

                    _probe.RemovedSnapshot(snapshotId);
                }

                var contentUrisToDelete = remoteContentUris
                    .Except(localContentUris)
                    .ToArray();

                foreach (var contentUri in contentUrisToDelete)
                {
                    contentRepository.RemoveValue(contentUri);

                    _probe.RemovedContent(contentUri);
                }
            }
        }

        public void RetrieveContent(
            IEnumerable<Uri> contentUris,
            Stream outputStream)
        {
            contentUris.EnsureNotNull(nameof(contentUris));
            outputStream.EnsureNotNull(nameof(outputStream));

            foreach (var contentUri in contentUris)
            {
                try
                {
                    var decrypted = AesGcmCrypto.Decrypt(
                        _uriRepository.RetrieveValue(contentUri),
                        _key.Value);

                    outputStream.Write(decrypted);
                }
                catch (CryptographicException e)
                {
                    throw new ChunkyardException(
                        $"Could not decrypt content: {contentUri}",
                        e);
                }
            }
        }

        private IEnumerable<Uri> FetchContentUris(IEnumerable<int> snapshotIds)
        {
            var contentUris = new HashSet<Uri>();

            foreach (var snapshotId in snapshotIds)
            {
                var snapshotReference = GetSnapshotReference(snapshotId);

                contentUris.UnionWith(
                    snapshotReference.ContentUris);

                var snapshot = GetSnapshot(
                    snapshotId,
                    snapshotReference);

                contentUris.UnionWith(
                    snapshot.BlobReferences.SelectMany(
                        blobReference => blobReference.ContentUris));
            }

            return contentUris;
        }

        private static BlobReference[] Filter(
            Snapshot snapshot,
            Fuzzy includeFuzzy)
        {
            return snapshot.BlobReferences
                .Where(br => includeFuzzy.IsMatch(br.Name))
                .ToArray();
        }

        private int? FetchCurrentSnapshotId()
        {
            return _intRepository.ListKeys()
                .OrderBy(i => i)
                .Select(i => i as int?)
                .LastOrDefault();
        }

        private IReadOnlyCollection<Uri> WriteContent(
            byte[] nonce,
            Stream stream)
        {
            // Let's create the key here instead of inside the WriteChunk
            // method. This ensures that a key is generated as soon as possible,
            // even if we ingest an empty file (in which case WriteChunk would
            // not be called)
            var key = _key.Value;

            Uri WriteChunk(byte[] chunk)
            {
                var encryptedData = AesGcmCrypto.Encrypt(
                    nonce,
                    chunk,
                    key);

                var contentUri = Id.ComputeContentUri(encryptedData);

                _uriRepository.StoreValue(contentUri, encryptedData);

                return contentUri;
            }

            var expectedChunks = stream.Length / _fastCdc.AvgSize;

            if (expectedChunks < _parallelizeChunkThreshold)
            {
                return _fastCdc.SplitIntoChunks(stream)
                    .Select(WriteChunk)
                    .ToArray();
            }
            else
            {
                return _fastCdc.SplitIntoChunks(stream)
                    .AsParallel()
                    .AsOrdered()
                    .Select(WriteChunk)
                    .ToArray();
            }
        }

        private BlobReference[] WriteBlobs(
            IBlobReader blobReader)
        {
            var currentBlobReferences = _currentSnapshotId == null
                ? new Dictionary<string, BlobReference>()
                : GetSnapshot(_currentSnapshotId.Value).BlobReferences
                    .ToDictionary(br => br.Name, br => br);

            return blobReader.FetchBlobs()
                .AsParallel()
                .Select(blob =>
                {
                    currentBlobReferences.TryGetValue(
                        blob.Name,
                        out var current);

                    if (current != null
                        && current.ToBlob().Equals(blob))
                    {
                        return current;
                    }

                    // Known blobs should be encrypted using the same nonce
                    var nonce = current?.Nonce
                        ?? AesGcmCrypto.GenerateNonce();

                    using var stream = blobReader.OpenRead(blob.Name);

                    var blobReference = new BlobReference(
                        blob.Name,
                        blob.LastWriteTimeUtc,
                        nonce,
                        WriteContent(nonce, stream));

                    _probe.StoredBlob(blobReference);

                    return blobReference;
                })
                .OrderBy(blobReference => blobReference.Name)
                .ToArray();
        }

        private int ResolveSnapshotId(int snapshotId)
        {
            //  0: the first element
            //  1: the second element
            // -1: the last element
            // -2: the second-last element
            if (snapshotId >= 0)
            {
                return snapshotId;
            }

            var snapshotIds = _intRepository.ListKeys()
                .OrderBy(i => i)
                .ToArray();

            if (snapshotIds.Length == 0)
            {
                throw new ChunkyardException(
                    "Cannot operate on an empty repository");
            }

            var index = snapshotIds.Length + snapshotId;

            if (index < 0)
            {
                throw new ChunkyardException(
                    $"Could not resolve snapshot: #{snapshotId}");
            }

            return snapshotIds[index];
        }

        private Snapshot GetSnapshot(
            int snapshotId,
            SnapshotReference snapshotReference)
        {
            try
            {
                using var memoryStream = new MemoryStream();

                RetrieveContent(
                    snapshotReference.ContentUris,
                    memoryStream);

                return DataConvert.BytesToObject<Snapshot>(
                    memoryStream.ToArray());
            }
            catch (ChunkyardException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ChunkyardException(
                    $"Could not read snapshot: #{snapshotId}",
                    e);
            }
        }

        private SnapshotReference GetSnapshotReference(int snapshotId)
        {
            try
            {
                return DataConvert.BytesToObject<SnapshotReference>(
                    _intRepository.RetrieveValue(snapshotId));
            }
            catch (Exception e)
            {
                throw new ChunkyardException(
                    $"Could not read snapshot reference: #{snapshotId}",
                    e);
            }
        }
    }
}
