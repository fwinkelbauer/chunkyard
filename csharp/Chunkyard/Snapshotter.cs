using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Serilog;

namespace Chunkyard
{
    public class Snapshotter<T> where T : IContentRef
    {
        private const string ContentName = "snapshot";

        private static readonly ILogger _log = Log.ForContext<Snapshotter<T>>();

        private readonly IRepository _repository;
        private readonly IContentStore<T> _store;
        private readonly HashAlgorithmName _hashAlgorithmName;

        public Snapshotter(IRepository repository, IContentStore<T> store, HashAlgorithmName hashAlgorithmName)
        {
            _repository = repository;
            _store = store;
            _hashAlgorithmName = hashAlgorithmName;
        }

        public int Write(string logName, DateTime creationTime, IEnumerable<string> filePaths, Func<string, Stream> readFunc)
        {
            if (_repository.TryFetchLogPosition(logName, out var currentLogPosition))
            {
                var oldSnapshot = ParseSnapshotRef(
                    _repository.RetrieveFromLog<T>(logName, currentLogPosition));

                foreach (var contentRef in oldSnapshot.ContentRefs)
                {
                    // Known files should be encrypted using the existing
                    // parameters, so we register all previous references
                    _store.Visit(contentRef);
                }
            }

            var snapshot = new Snapshot<T>(creationTime, WriteFiles(filePaths, readFunc));
            var serialized = DataConvert.SerializeObject(snapshot);
            var snapshotRef = _store.StoreUtf8(serialized, _hashAlgorithmName, ContentName);

            return _repository.AppendToLog(snapshotRef, logName, currentLogPosition);
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

        private Snapshot<T> RetrieveSnapshot(Uri snapshotUri)
        {
            return ParseSnapshotRef(
                _repository.RetrieveFromLog<T>(snapshotUri));
        }

        private Snapshot<T> ParseSnapshotRef(T snapshotRef)
        {
            _store.ThrowIfInvalid(snapshotRef);

            return DataConvert.DeserializeObject<Snapshot<T>>(
                _store.RetrieveUtf8(snapshotRef));
        }

        private IEnumerable<T> WriteFiles(IEnumerable<string> filePaths, Func<string, Stream> readFunc)
        {
            foreach (var filePath in filePaths)
            {
                _log.Information("Saving: {File}", filePath);
                using var stream = readFunc(filePath);
                yield return _store.Store(stream, _hashAlgorithmName, filePath);
            }
        }
    }
}
