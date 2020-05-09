using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Serilog;

namespace Chunkyard
{
    internal class SnapshotBuilder
    {
        private const string SnapshotContentName = "snapshot";
        private const int Iterations = 1000;

        private static readonly ILogger _log =
            Log.ForContext<SnapshotBuilder>();

        private readonly IContentStore _contentStore;
        private readonly HashAlgorithmName _hashAlgorithmName;
        private readonly int _minChunkSizeInByte;
        private readonly int _avgChunkSizeInByte;
        private readonly int _maxChunkSizeInByte;
        private readonly KeyInformation _key;
        private readonly Dictionary<string, byte[]> _noncesByFingerprints;
        private readonly List<ContentReference> _storedContentReferences;

        private int? _currentLogPosition;

        // TODO Parameter vereinfachen. Auch für Config Objekte
        private SnapshotBuilder(
            IContentStore contentStore,
            HashAlgorithmName hashAlgorithmName,
            int minChunkSizeInByte,
            int avgChunkSizeInByte,
            int maxChunkSizeInByte,
            KeyInformation key,
            int? currentLogPosition,
            Dictionary<string, byte[]> noncesByFingerprints)
        {
            _contentStore = contentStore;
            _hashAlgorithmName = hashAlgorithmName;
            _minChunkSizeInByte = minChunkSizeInByte;
            _avgChunkSizeInByte = avgChunkSizeInByte;
            _maxChunkSizeInByte = maxChunkSizeInByte;
            _key = key;
            _currentLogPosition = currentLogPosition;
            _noncesByFingerprints = noncesByFingerprints;
            _storedContentReferences = new List<ContentReference>();
        }

        public void AddContent(Stream inputStream, string contentName)
        {
            _storedContentReferences.Add(_contentStore.StoreContent(
                inputStream,
                new StoreConfig(
                    contentName,
                    _hashAlgorithmName,
                    GenerateNonce,
                    _key.Key,
                    _minChunkSizeInByte,
                    _avgChunkSizeInByte,
                    _maxChunkSizeInByte)));
        }

        public void WriteSnapshot(DateTime creationTime)
        {
            var contentReference = _contentStore.StoreContent(
                new Snapshot(creationTime, _storedContentReferences),
                new StoreConfig(
                    SnapshotContentName,
                    _hashAlgorithmName,
                    GenerateNonce,
                    _key.Key,
                    _minChunkSizeInByte,
                    _avgChunkSizeInByte,
                    _maxChunkSizeInByte));

            _currentLogPosition = _contentStore.AppendToLog(
                new SnapshotReference(
                    contentReference,
                    _key.Salt,
                    _key.Iterations),
                _currentLogPosition);
        }

        public void RestoreSnapshot(
            int restoreLogPosition,
            Func<string, Stream> writeFunc,
            string restoreFuzzy)
        {
            if (!_currentLogPosition.HasValue)
            {
                throw new ChunkyardException(
                    "Cannot restore snapshot from an empty repository");
            }

            var snapshotReference = _contentStore
                .RetrieveFromLog<SnapshotReference>(
                    restoreLogPosition);

            var snapshot = _contentStore.RetrieveContent<Snapshot>(
                snapshotReference.ContentReference,
                new RetrieveConfig(_key.Key));

            var index = 1;
            var filteredContentReferences = FuzzyFilter(
                restoreFuzzy,
                snapshot.ContentReferences)
                .ToArray();

            foreach (var contentReference in filteredContentReferences)
            {
                _log.Information(
                    "Restoring: {File} ({CurrentIndex}/{MaxIndex})",
                    contentReference.Name,
                    index++,
                    filteredContentReferences.Length);

                using var stream = writeFunc(contentReference.Name);
                _contentStore.RetrieveContent(
                    contentReference,
                    new RetrieveConfig(_key.Key),
                    stream);
            }
        }

        private byte[] GenerateNonce(string fingerprint)
        {
            if (!_noncesByFingerprints.TryGetValue(fingerprint, out var nonce))
            {
                nonce = AesGcmCrypto.GenerateNonce();
                _noncesByFingerprints[fingerprint] = nonce;
            }

            return nonce;
        }

        private static IEnumerable<ContentReference> FuzzyFilter(
            string fuzzyPattern,
            IEnumerable<ContentReference> contentReferences)
        {
            var fuzzy = new Fuzzy(fuzzyPattern);

            foreach (var contentReference in contentReferences)
            {
                if (fuzzy.IsMatch(contentReference.Name))
                {
                    yield return contentReference;
                }
            }
        }

        public static SnapshotBuilder Create(
            IPrompt prompt,
            IContentStore contentStore,
            HashAlgorithmName hashAlgorithmName,
            int minChunkSizeInByte,
            int avgChunkSizeInByte,
            int maxChunkSizeInByte)
        {
            var currentLogPosition = contentStore.FetchLogPosition();
            KeyInformation? key = null;
            var noncesByFingerprints = new Dictionary<string, byte[]>();

            if (currentLogPosition.HasValue)
            {
                var snapshotReference = contentStore
                    .RetrieveFromLog<SnapshotReference>(
                        currentLogPosition.Value);

                key = AesGcmCrypto.PasswordToKey(
                    prompt.ExistingPassword(),
                    snapshotReference.Salt,
                    snapshotReference.Iterations);

                var snapshot = contentStore.RetrieveContent<Snapshot>(
                    snapshotReference.ContentReference,
                    new RetrieveConfig(key.Key));

                // Known chunks should be encrypted using the existing
                // parameters, so we register all previous references
                foreach (var contentReference in snapshot.ContentReferences)
                {
                    foreach (var chunk in contentReference.Chunks)
                    {
                        noncesByFingerprints[chunk.Fingerprint] =
                            chunk.Nonce;
                    }
                }
            }
            else
            {
                key = AesGcmCrypto.PasswordToKey(
                    prompt.NewPassword(),
                    AesGcmCrypto.GenerateSalt(),
                    Iterations);
            }

            return new SnapshotBuilder(
                contentStore,
                hashAlgorithmName,
                minChunkSizeInByte,
                avgChunkSizeInByte,
                maxChunkSizeInByte,
                key,
                currentLogPosition,
                noncesByFingerprints);
        }

/*      public void VerifySnapshot(
            Uri snapshotUri,
            string verifyFuzzy,
            bool shallow)
        {
            var (snapshot, _) = RetrieveSnapshot(
                snapshotUri,
                _prompt.ExistingPassword());

            var index = 1;
            var filteredContentReferences = FuzzyFilter(
                verifyFuzzy,
                snapshot.ContentReferences)
                .ToArray();

            foreach (var contentReference in filteredContentReferences)
            {
                _log.Information(
                    "Verifying: {File} ({CurrentIndex}/{MaxIndex})",
                    contentReference.Name,
                    index++,
                    filteredContentReferences.Length);

                foreach (var chunk in contentReference.Chunks)
                {
                    if (shallow)
                    {
                        _contentStore.Repository.ThrowIfNotExists(
                            chunk.ContentUri);
                    }
                    else
                    {
                        _contentStore.Repository.ThrowIfInvalid(
                            chunk.ContentUri);
                    }
                }
            }
        }

        public Snapshot GetSnapshot(Uri snapshotUri)
        {
            var (snapshot, _) = RetrieveSnapshot(
                snapshotUri,
                _prompt.ExistingPassword());

            return snapshot;
        }

        private Snapshot RetrieveSnapshot(Uri snapshotUri)
        {
            var snapshotReference = _contentStore.Repository
                .RetrieveFromLog(snapshotUri)
                .ToObject<SnapshotReference>();

            return ParseSnapshot(snapshotReference);
        }

        private Snapshot ParseSnapshot(SnapshotReference snapshotReference)
        {
            using var memoryBuffer = new MemoryStream();
            _contentStore.RetrieveContent(snapshotReference.ContentReference, memoryBuffer, BuildKey());

            return memoryBuffer.ToArray().ToObject<Snapshot>();
        }*/
    }
}
