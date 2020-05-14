﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Chunkyard.Options;
using Newtonsoft.Json;
using Serilog;

namespace Chunkyard
{
    internal class Command
    {
        public const int LatestLogPosition = -1;

        private static readonly string CacheDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "chunkyard",
            "cache");

        private static readonly ILogger _log = Log.ForContext<Command>();

        public static void PreviewFiles(PreviewOptions o)
        {
            var found = FileFetcher.Find(o.Files, o.ExcludePatterns);

            foreach ((_, var contentName) in found)
            {
                Console.WriteLine(contentName);
            }
        }

        public static void CreateSnapshot(CreateOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(o.Repository, o.Cached);

            _log.Information("Creating new snapshot");

            var index = 1;
            var found = FileFetcher.Find(o.Files, o.ExcludePatterns)
                .ToList();

            foreach ((var foundFile, var contentName) in found)
            {
                _log.Information(
                    "Storing: {Content} ({CurrentIndex}/{MaxIndex})",
                    contentName,
                    index++,
                    found.Count);

                using var fileStream = File.OpenRead(foundFile);
                snapshotBuilder.AddContent(fileStream, contentName);
            }

            var newLogPosition = snapshotBuilder.WriteSnapshot(DateTime.Now);

            _log.Information(
                "Latest snapshot is {LogPosition}",
                newLogPosition);
        }

        public static void VerifySnapshot(VerifyOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(
                o.Repository);

            _log.Information("Verifying snapshot {LogPosition}", o.LogPosition);

            var snapshot = snapshotBuilder.GetSnapshot(o.LogPosition);

            var index = 1;
            var filteredContentReferences = FuzzyFilter(
                o.IncludeFuzzy,
                snapshot.ContentReferences);

            var error = false;

            foreach (var contentReference in filteredContentReferences)
            {
                if (!snapshotBuilder.ContentStore.ContentExists(contentReference))
                {
                    _log.Warning(
                        "Missing: {Content} ({CurrentIndex}/{MaxIndex})",
                        contentReference.Name,
                        index++,
                        filteredContentReferences.Count);

                    error |= true;
                }
                else if (!o.Shallow && !snapshotBuilder.ContentStore.ContentValid(contentReference))
                {
                    _log.Warning(
                        "Corrupted: {Content} ({CurrentIndex}/{MaxIndex})",
                        contentReference.Name,
                        index++,
                        filteredContentReferences.Count);

                    error |= true;
                }
                else
                {
                    _log.Information(
                        "Valid: {File} ({CurrentIndex}/{MaxIndex})",
                        contentReference.Name,
                        index++,
                        filteredContentReferences.Count);
                }
            }

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

            var index = 1;
            var filteredContentReferences = FuzzyFilter(
                o.IncludeFuzzy,
                snapshot.ContentReferences);

            foreach (var contentReference in filteredContentReferences)
            {
                var file = Path.Combine(
                    o.Directory,
                    contentReference.Name);

                _log.Information(
                    "Restoring: {File} ({CurrentIndex}/{MaxIndex})",
                    file,
                    index++,
                    filteredContentReferences.Count);

                Directory.CreateDirectory(Path.GetDirectoryName(file));
                using var stream = new FileStream(file, mode, FileAccess.Write);

                snapshotBuilder.ContentStore.RetrieveContent(
                    contentReference,
                    snapshotBuilder.Key.Key,
                    stream);
            }
        }

        public static void ShowLogPositions(LogOptions o)
        {
            var snapshots = CreateSnapshotBuilder(o.Repository)
                .GetSnapshots();

            foreach (var snapshotTuple in snapshots)
            {
                var logPosition = snapshotTuple.Item1;
                var snapshot = snapshotTuple.Item2;

                Console.WriteLine($"{logPosition}: {snapshot.CreationTime}");
            }
        }

        private static SnapshotBuilder CreateSnapshotBuilder(
            string repository,
            bool cached = false)
        {
            _log.Information("Using repository {Repository}", repository);

            var nonceGenerator = new NonceGenerator();
            IContentStore contentStore = new ContentStore(
                new FileRepository(repository),
                nonceGenerator,
                new ContentStoreConfig(
                    HashAlgorithmName.SHA256,
                    2 * 1024 * 1024,
                    4 * 1024 * 1024,
                    8 * 1024 * 1024));

            contentStore = cached
                ? new CachedContentStore(contentStore, CacheDirectoryPath)
                : contentStore;

            return SnapshotBuilder.OpenRepository(
                new EnvironmentPrompt(
                    new ConsolePrompt()),
                nonceGenerator,
                contentStore);
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
