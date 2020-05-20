﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Chunkyard.Options;
using Serilog;

namespace Chunkyard
{
    internal class Command
    {
        public const int LatestLogPosition = -1;

        private static readonly string CacheDirectoryPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "chunkyard",
            "cache");

        private static readonly ILogger _log = Log.ForContext<Command>();

        public static void PreviewFiles(PreviewOptions o)
        {
            var foundTuples = FileFetcher.Find(o.Files, o.ExcludePatterns);

            foreach ((_, var contentName) in foundTuples)
            {
                Console.WriteLine(contentName);
            }
        }

        public static void CreateSnapshot(CreateOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(o.Repository, o.Cached);

            _log.Information("Creating new snapshot");

            var foundTuples = FileFetcher.Find(o.Files, o.ExcludePatterns)
                .ToList();

            Parallel.ForEach(
                foundTuples,
                t =>
                {
                    using var fileStream = File.OpenRead(t.FoundFile);
                    snapshotBuilder.AddContent(fileStream, t.ContentName);

                    _log.Information("Stored: {Content}", t.ContentName);
                });

            var newLogPosition = snapshotBuilder.WriteSnapshot(DateTime.Now);

            _log.Information(
                "Latest snapshot is {LogPosition}",
                newLogPosition);
        }

        public static void CheckSnapshot(CheckOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(
                o.Repository);

            _log.Information("Checking snapshot {LogPosition}", o.LogPosition);

            var snapshot = snapshotBuilder.GetSnapshot(o.LogPosition);
            var filteredContentReferences = FuzzyFilter(
                o.IncludeFuzzy,
                snapshot.ContentReferences);

            var error = false;

            Parallel.ForEach(
                filteredContentReferences,
                contentReference =>
                {
                    if (!snapshotBuilder.ContentStore
                        .ContentExists(contentReference))
                    {
                        _log.Warning(
                            "Missing: {Content}",
                            contentReference.Name);

                        error = true;
                    }
                    else if (!o.Shallow
                        && !snapshotBuilder.ContentStore
                             .ContentValid(contentReference))
                    {
                        _log.Warning(
                            "Corrupted: {Content}",
                            contentReference.Name);

                        error = true;
                    }
                    else
                    {
                        _log.Information(
                            "Validated: {File}",
                            contentReference.Name);
                    }
                });

            if (error)
            {
                throw new ChunkyardException(
                    "Detected errors while verifying snapshot");
            }
        }

        public static void ListSnapshot(ListOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(
                o.Repository);

            _log.Information("Listing snapshot {LogPosition}", o.LogPosition);

            var snapshot = snapshotBuilder.GetSnapshot(o.LogPosition);
            var filteredContentReferences = FuzzyFilter(
                o.IncludeFuzzy,
                snapshot.ContentReferences);

            foreach (var contentReference in filteredContentReferences)
            {
                Console.WriteLine(contentReference.Name);
            }
        }

        public static void RestoreSnapshot(RestoreOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(
                o.Repository);

            _log.Information("Restoring snapshot {LogPosition}", o.LogPosition);

            var snapshot = snapshotBuilder.GetSnapshot(o.LogPosition);
            var mode = o.Overwrite
                ? FileMode.OpenOrCreate
                : FileMode.CreateNew;

            var filteredContentReferences = FuzzyFilter(
                o.IncludeFuzzy,
                snapshot.ContentReferences);

            var error = false;

            Parallel.ForEach(
                filteredContentReferences,
                contentReference =>
                {
                    var file = Path.Combine(
                        o.Directory,
                        contentReference.Name);

                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(file));

                        using var stream = new FileStream(
                        file,
                        mode,
                        FileAccess.Write);

                        snapshotBuilder.RetrieveContent(
                            contentReference,
                            stream);

                        _log.Information(
                            "Restored: {File}",
                            file);

                    }
                    catch (Exception e)
                    {
                        _log.Error(e, "Error: {File}", file);

                        error = true;
                    }
                });

            if (error)
            {
                throw new ChunkyardException(
                    "Detected errors while restoring snapshot");
            }
        }

        public static void ShowLogPositions(LogOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(o.Repository);
            var logPositions = snapshotBuilder.ContentStore.ListLogPositions();

            foreach (var logPosition in logPositions)
            {
                var snapshot = snapshotBuilder.GetSnapshot(logPosition);

                _log.Information(
                    "{LogPosition}: {Time}",
                    logPosition,
                    snapshot.CreationTime);
            }
        }

        public static void GarbageCollect(GarbageCollectOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(o.Repository);
            var usedUris = new Dictionary<Uri, bool>();
            var allContentUris = snapshotBuilder.ContentStore.Repository
                .ListUris();

            foreach (var contentUri in allContentUris)
            {
                usedUris[contentUri] = false;
            }

            var logPositions = snapshotBuilder.ContentStore.ListLogPositions();

            foreach (var logPosition in logPositions)
            {
                var contentUris = snapshotBuilder.ListContents(logPosition);

                foreach (var contentUri in contentUris)
                {
                    usedUris[contentUri] = true;
                }
            }

            foreach (var contentUri in usedUris.Keys)
            {
                if (usedUris[contentUri])
                {
                    continue;
                }

                _log.Information("Unused: {ContentUri}", contentUri);

                if (!o.Preview)
                {
                    snapshotBuilder.ContentStore.Repository
                        .RemoveUri(contentUri);
                }
            }
        }

        private static SnapshotBuilder CreateSnapshotBuilder(
            string repositoryPath,
            bool cached = false)
        {
            var repository = new FileRepository(repositoryPath);
            var logPosition = ContentStore.FetchLogPosition(repository);
            var prompt = new EnvironmentPrompt(
                new ConsolePrompt());

            string? password = null;
            byte[]? salt = null;
            int? iterations = null;
            LogReference? logReference = null;

            if (logPosition == null)
            {
                password = prompt.NewPassword();
                salt = AesGcmCrypto.GenerateSalt();
                iterations = AesGcmCrypto.Iterations;
            }
            else
            {
                logReference = ContentStore.RetrieveFromLog(
                    repository,
                    logPosition.Value);

                password = prompt.ExistingPassword();
                salt = logReference.Salt;
                iterations = logReference.Iterations;
            }

            var contentStore = new ContentStore(
                repository,
                new FastCdc(
                    2 * 1024 * 1024,
                    4 * 1024 * 1024,
                    8 * 1024 * 1024),
                HashAlgorithmName.SHA256,
                password,
                salt,
                iterations.Value);

            if (logReference != null)
            {
                var snapshot = contentStore.RetrieveContent<Snapshot>(
                    logReference.ContentReference);

                // Known files should be encrypted using the existing
                // parameters, so we register all previous references
                foreach (var contentReference in snapshot.ContentReferences)
                {
                    contentStore.RegisterNonce(
                        contentReference.Name,
                        contentReference.Nonce);
                }
            }

            IContentStore nestedContentStore = contentStore;

            if (cached)
            {
                nestedContentStore = new CachedContentStore(
                    contentStore,
                    CacheDirectoryPath);
            }

            return new SnapshotBuilder(
                nestedContentStore,
                logPosition);
        }

        private static List<ContentReference> FuzzyFilter(
            string fuzzyPattern,
            IEnumerable<ContentReference> contentReferences)
        {
            var fuzzy = new Fuzzy(fuzzyPattern);
            var matches = new List<ContentReference>();

            foreach (var contentReference in contentReferences)
            {
                if (fuzzy.IsMatch(contentReference.Name))
                {
                    matches.Add(contentReference);
                }
            }

            return matches;
        }
    }
}
