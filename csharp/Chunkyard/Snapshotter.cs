﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Chunkyard.Core;
using Serilog;

namespace Chunkyard
{
    public class Snapshotter<T> where T : IContentRef
    {
        private const string ContentName = "snapshot";

        private static readonly ILogger _log = Log.ForContext<Snapshotter<T>>();

        private readonly IContentStore<T> _store;

        public Snapshotter(IContentStore<T> store)
        {
            _store = store;
        }

        public int Write(string logName, DateTime creationTime, IEnumerable<string> filePaths, Func<string, Stream> readFunc, HashAlgorithmName hashAlgorithmName)
        {
            var currentLogPosition = _store.Repository.FetchLogPosition(logName);

            if (currentLogPosition.HasValue)
            {
                var oldSnapshot = ParseSnapshotRef(
                    _store.Repository.RetrieveFromLog<T>(logName, currentLogPosition.Value));

                foreach (var contentRef in oldSnapshot.ContentRefs)
                {
                    // Known files should be encrypted using the existing
                    // parameters, so we register all previous references
                    _store.Visit(contentRef);
                }
            }

            var snapshot = new Snapshot<T>(creationTime, WriteFiles(filePaths, readFunc, hashAlgorithmName));
            var serialized = DataConvert.SerializeObject(snapshot);
            var snapshotRef = _store.StoreUtf8(serialized, hashAlgorithmName, ContentName);

            return _store.Repository.AppendToLog(snapshotRef, logName, currentLogPosition);
        }

        public void Restore(Uri snapshotUri, Func<T, Stream> writeFunc, string restoreRegex)
        {
            var snapshot = RetrieveSnapshot(snapshotUri);
            var compiledRegex = new Regex(restoreRegex);

            foreach (var contentRef in snapshot.ContentRefs)
            {
                if (compiledRegex.IsMatch(contentRef.Name))
                {
                    _log.Information("Restoring: {File}", contentRef.Name);
                    using var stream = writeFunc(contentRef);
                    _store.Retrieve(stream, contentRef);
                }
            }
        }

        public IEnumerable<string> List(Uri snapshotUri, string listRegex)
        {
            var snapshot = RetrieveSnapshot(snapshotUri);
            var compiledRegex = new Regex(listRegex);

            foreach (var contentRef in snapshot.ContentRefs)
            {
                if (compiledRegex.IsMatch(contentRef.Name))
                {
                    yield return contentRef.Name;
                }
            }
        }

        public void Verify(Uri snapshotUri)
        {
            var snapshot = RetrieveSnapshot(snapshotUri);

            foreach (var contentRef in snapshot.ContentRefs)
            {
                _log.Information("Verifying: {File}", contentRef.Name);
                _store.ThrowIfInvalid(contentRef);
            }
        }

        public void Push(string logName, IRepository destinationRepository)
        {
            var sourceLogPosition = _store.Repository.FetchLogPosition(logName);
            var destinationLogPosition = destinationRepository.FetchLogPosition(logName);

            if (!sourceLogPosition.HasValue)
            {
                _log.Information("Cannot push an empty log");
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
                var serializedSource = DataConvert.SerializeObject(
                    _store.Repository.RetrieveFromLog<T>(logName, i));

                var serializedDestination = DataConvert.SerializeObject(
                    destinationRepository.RetrieveFromLog<T>(logName, i));

                if (!serializedSource.Equals(serializedDestination))
                {
                    throw new ChunkyardException($"Logs differ at position {i}");
                }
            }

            for (int i = commonLogPosition + 1; i <= sourceLogPosition; i++)
            {
                _log.Information("Pushing snapshot with position: {LogPosition}", i);

                var snapshotRef = _store.Repository.RetrieveFromLog<T>(logName, i);
                PushSnapshot(snapshotRef, destinationRepository);
                destinationRepository.AppendToLog(snapshotRef, logName, i - 1);
            }
        }

        private void PushSnapshot(T snapshotRef, IRepository remoteRepository)
        {
            var snapshot = ParseSnapshotRef(snapshotRef);

            foreach (var contentRef in snapshot.ContentRefs)
            {
                _log.Information("Pushing content: {File}", contentRef.Name);

                foreach (var contentUri in _store.ListContentUris(contentRef))
                {
                    _store.Repository.PushContent(contentUri, remoteRepository);
                }
            }

            foreach (var contentUri in _store.ListContentUris(snapshotRef))
            {
                _store.Repository.PushContent(contentUri, remoteRepository);
            }
        }

        private Snapshot<T> RetrieveSnapshot(Uri snapshotUri)
        {
            return ParseSnapshotRef(
                _store.Repository.RetrieveFromLog<T>(snapshotUri));
        }

        private Snapshot<T> ParseSnapshotRef(T snapshotRef)
        {
            return DataConvert.DeserializeObject<Snapshot<T>>(
                _store.RetrieveUtf8(snapshotRef));
        }

        private IEnumerable<T> WriteFiles(IEnumerable<string> filePaths, Func<string, Stream> readFunc, HashAlgorithmName hashAlgorithmName)
        {
            foreach (var filePath in filePaths)
            {
                _log.Information("Saving: {File}", filePath);
                using var stream = readFunc(filePath);
                yield return _store.Store(stream, hashAlgorithmName, filePath);
            }
        }
    }
}
