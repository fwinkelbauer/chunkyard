﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;

namespace Chunkyard
{
    internal class SnapshotBuilder
    {
        private const string SnapshotContentName = "snapshot";
        private const int Iterations = 1000;

        private static readonly ILogger _log = Log.ForContext<SnapshotBuilder>();

        private readonly IContentStore _contentStore;
        private readonly IPrompt _prompt;
        private readonly List<(Func<Stream>, string)> _contentItems;
        private readonly Dictionary<string, byte[]> _noncesByName;

        public SnapshotBuilder(IContentStore contentStore, IPrompt prompt)
        {
            _contentStore = contentStore;
            _prompt = prompt;
            _contentItems = new List<(Func<Stream>, string)>();
            _noncesByName = new Dictionary<string, byte[]>();
        }

        public void AddContent(Func<Stream> readFunc, string contentName)
        {
            _contentItems.Add((readFunc, contentName));
        }

        public int WriteSnapshot(string logName, DateTime creationTime, ChunkyardConfig config)
        {
            var currentLogPosition = _contentStore.Repository.FetchLogPosition(logName);
            var password = currentLogPosition.HasValue
                ? _prompt.ExistingPassword()
                : _prompt.NewPassword();

            var salt = AesGcmCrypto.GenerateSalt();
            var iterations = Iterations;
            var key = AesGcmCrypto.PasswordToKey(password, salt, iterations);

            if (currentLogPosition.HasValue)
            {
                var snapshotTuple = RetrieveSnapshotReference(
                    logName,
                    currentLogPosition.Value,
                    password);

                var currentSnapshotReference = snapshotTuple.Item1;
                salt = currentSnapshotReference.Salt;
                iterations = currentSnapshotReference.Iterations;
                key = snapshotTuple.Item2;
                var currentSnapshot = ParseSnapshot(currentSnapshotReference, key);

                foreach (var contentReference in currentSnapshot.ContentReferences)
                {
                    // Known files should be encrypted using the existing
                    // parameters, so we register all previous references
                    _noncesByName[contentReference.Name] = contentReference.Nonce;
                }
            }

            var snapshot = new Snapshot(creationTime, StoreContentItems(key, config));
            using var snapshotStream = new MemoryStream(snapshot.ToBytes());
            var snapshotReference = SnapshotReference.FromContentReference(
                _contentStore.StoreContent(snapshotStream,
                    SnapshotContentName,
                    GetNonce(SnapshotContentName),
                    key,
                    config),
                salt,
                iterations);

            return _contentStore.Repository.AppendToLog(
                snapshotReference.ToBytes(),
                logName,
                currentLogPosition);
        }

        public void RestoreSnapshot(Uri snapshotUri, Func<string, Stream> writeFunc, string restoreFuzzy)
        {
            var (snapshot, key) = RetrieveSnapshot(
                snapshotUri,
                _prompt.ExistingPassword());

            var index = 1;
            var filteredContentReferences = FuzzyFilter(restoreFuzzy, snapshot.ContentReferences)
                .ToArray();

            foreach (var contentReference in filteredContentReferences)
            {
                _log.Information(
                    "Restoring: {File} ({CurrentIndex}/{MaxIndex})",
                    contentReference.Name,
                    index++,
                    filteredContentReferences.Length);

                using var stream = writeFunc(contentReference.Name);
                _contentStore.RetrieveContent(contentReference, stream, key);
            }
        }

        public void VerifySnapshot(Uri snapshotUri, string verifyFuzzy, bool shallow)
        {
            var (snapshot, _) = RetrieveSnapshot(
                snapshotUri,
                _prompt.ExistingPassword());

            var index = 1;
            var filteredContentReferences = FuzzyFilter(verifyFuzzy, snapshot.ContentReferences)
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
                        _contentStore.Repository.ThrowIfNotExists(chunk.ContentUri);
                    }
                    else
                    {
                        _contentStore.Repository.ThrowIfInvalid(chunk.ContentUri);
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

        public void Push(string logName, IRepository destinationRepository)
        {
            var password = _prompt.ExistingPassword();
            var sourceLogPosition = _contentStore.Repository.FetchLogPosition(logName);
            var destinationLogPosition = destinationRepository.FetchLogPosition(logName);

            if (!sourceLogPosition.HasValue)
            {
                _log.Information("Cannot process an empty log");
                return;
            }

            var commonLogPosition = destinationLogPosition.HasValue
                ? Math.Min(sourceLogPosition.Value, destinationLogPosition.Value)
                : -1;

            if (commonLogPosition == sourceLogPosition)
            {
                _log.Information("Already up-to-date");
                return;
            }

            for (int i = 0; i <= commonLogPosition; i++)
            {
                var source = _contentStore.Repository.RetrieveFromLog(logName, i);
                var destination = destinationRepository.RetrieveFromLog(logName, i);

                if (!Enumerable.SequenceEqual(source, destination))
                {
                    throw new ChunkyardException($"Logs differ at position {i}");
                }
            }

            for (int i = commonLogPosition + 1; i <= sourceLogPosition; i++)
            {
                _log.Information("Processing snapshot ({CurrentIndex}/{MaxIndex})", i, sourceLogPosition);

                var (snapshotReference, key) = RetrieveSnapshotReference(
                    logName,
                    i,
                    password);

                PushSnapshot(
                    snapshotReference,
                    destinationRepository,
                    key);

                destinationRepository.AppendToLog(
                    snapshotReference.ToBytes(),
                    logName,
                    i - 1);
            }
        }

        private Snapshot ParseSnapshot(ContentReference snapshotReference, byte[] key)
        {
            using var memoryBuffer = new MemoryStream();
            _contentStore.RetrieveContent(snapshotReference, memoryBuffer, key);

            return memoryBuffer.ToArray().ToObject<Snapshot>();
        }

        private (SnapshotReference, byte[]) RetrieveSnapshotReference(string logName, int logPosition, string password)
        {
            var snapshotReference = _contentStore.Repository
                .RetrieveFromLog(logName, logPosition)
                .ToObject<SnapshotReference>();

            var key = AesGcmCrypto.PasswordToKey(
                password,
                snapshotReference.Salt,
                snapshotReference.Iterations);

            return (snapshotReference, key);
        }

        private (SnapshotReference, byte[]) RetrieveSnapshotReference(Uri snapshotUri, string password)
        {
            var snapshotReference = _contentStore.Repository
                .RetrieveFromLog(snapshotUri)
                .ToObject<SnapshotReference>();

            var key = AesGcmCrypto.PasswordToKey(
                password,
                snapshotReference.Salt,
                snapshotReference.Iterations);

            return (snapshotReference, key);
        }

        private (Snapshot, byte[]) RetrieveSnapshot(Uri snapshotUri, string password)
        {
            var (snapshotReference, key) = RetrieveSnapshotReference(
                snapshotUri,
                password);

            return (ParseSnapshot(snapshotReference, key), key);
        }

        private IEnumerable<ContentReference> StoreContentItems(byte[] key, ChunkyardConfig config)
        {
            for (int i = 0; i < _contentItems.Count; i++)
            {
                var (readFunc, contentName) = _contentItems[i];

                _log.Information(
                    "Storing: {Content} ({CurrentIndex}/{MaxIndex})",
                    contentName,
                    i + 1,
                    _contentItems.Count);

                using var stream = readFunc();
                yield return _contentStore.StoreContent(
                    stream,
                    contentName,
                    GetNonce(contentName),
                    key,
                    config);
            }
        }

        private void PushSnapshot(ContentReference snapshotReference, IRepository remoteRepository, byte[] key)
        {
            var snapshot = ParseSnapshot(snapshotReference, key);

            for (int i = 0; i < snapshot.ContentReferences.Length; i++)
            {
                var contentReferences = snapshot.ContentReferences[i];

                _log.Information(
                    "Pushing content: {Content} ({CurrentIndex}/{MaxIndex})",
                    contentReferences.Name,
                    i + 1,
                    snapshot.ContentReferences.Length);

                foreach (var chunk in contentReferences.Chunks)
                {
                    _contentStore.Repository.PushContent(chunk.ContentUri, remoteRepository);
                }
            }

            foreach (var chunk in snapshotReference.Chunks)
            {
                _contentStore.Repository.PushContent(chunk.ContentUri, remoteRepository);
            }
        }

        private byte[] GetNonce(string name)
        {
            if (_noncesByName.TryGetValue(name, out var nonce))
            {
                return nonce;
            }
            else
            {
                nonce = AesGcmCrypto.GenerateNonce();
                _noncesByName[name] = nonce;

                return nonce;
            }
        }

        public IEnumerable<ContentReference> FuzzyFilter(string fuzzyPattern, IEnumerable<ContentReference> contentReferences)
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
    }
}
