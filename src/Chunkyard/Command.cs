using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Chunkyard.Options;

namespace Chunkyard
{
    /// <summary>
    /// Describes every available command line verb of the Chunkyard assembly.
    /// </summary>
    internal class Command
    {
        public const int LatestLogPosition = -1;

        private static readonly string CacheDirectoryPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "chunkyard",
            "cache");

        public static void PreviewFiles(PreviewOptions o)
        {
            var foundTuples = FileFetcher.Find(o.Files, o.ExcludePatterns);

            foreach ((_, var contentName) in foundTuples)
            {
                Console.WriteLine(contentName);
            }
        }

        public static void PushSnapshots(PushOptions o)
        {
            PushSnapshots(o.SourceRepository, o.DestinationRepository);
        }

        public static void PullSnapshots(PullOptions o)
        {
            PushSnapshots(o.SourceRepository, o.DestinationRepository);
        }

        public static void CreateSnapshot(CreateOptions o)
        {
            var (_, contentStore, snapshotBuilder) = Create(
                o.Repository,
                o.Cached,
                new FastCdc(o.Min, o.Avg, o.Max));

            var foundTuples = FileFetcher
                .Find(o.Files, o.ExcludePatterns)
                .ToArray();

            if (foundTuples.Length == 0)
            {
                Console.WriteLine("Nothing to do");
                return;
            }

            foreach (var foundTuple in foundTuples)
            {
                using var fileStream = File.OpenRead(foundTuple.FoundFile);
                snapshotBuilder.AddContent(fileStream, foundTuple.ContentName);

                Console.WriteLine($"Stored: {foundTuple.ContentName}");
            }

            var newLogPosition = snapshotBuilder.WriteSnapshot(DateTime.Now);
            var newSnapshot = snapshotBuilder.GetSnapshot(newLogPosition);

            // Perform a shallow check to make sure that our new snapshot is
            // alright
            foreach (var contentReference in newSnapshot.ContentReferences)
            {
                if (!contentStore.ContentExists(contentReference))
                {
                    throw new ChunkyardException(
                        "Detected errors while creating snapshot");
                }
            }

            Console.WriteLine($"Created snapshot: {newLogPosition}");
        }

        public static void CheckSnapshot(CheckOptions o)
        {
            var (_, contentStore, snapshotBuilder) = Create(o.Repository);

            var snapshot = snapshotBuilder.GetSnapshot(o.LogPosition);
            var filteredContentReferences = FuzzyFilter(
                o.IncludeFuzzy,
                snapshot.ContentReferences);

            var error = false;

            foreach (var contentReference in filteredContentReferences)
            {
                if (!contentStore.ContentExists(contentReference))
                {
                    Console.WriteLine($"Missing: {contentReference.Name}");

                    error = true;
                }
                else if (!o.Shallow
                    && !contentStore.ContentValid(contentReference))
                {
                    Console.WriteLine($"Corrupted: {contentReference.Name}");

                    error = true;
                }
                else
                {
                    Console.WriteLine($"Validated: {contentReference.Name}");
                }
            }

            if (error)
            {
                throw new ChunkyardException(
                    "Detected errors while verifying snapshot");
            }
        }

        public static void ShowSnapshot(ShowOptions o)
        {
            var (_, _, snapshotBuilder) = Create(o.Repository);

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
            var (_, contentStore, snapshotBuilder) = Create(o.Repository);

            var snapshot = snapshotBuilder.GetSnapshot(o.LogPosition);
            var mode = o.Overwrite
                ? FileMode.OpenOrCreate
                : FileMode.CreateNew;

            var filteredContentReferences = FuzzyFilter(
                o.IncludeFuzzy,
                snapshot.ContentReferences);

            var error = false;

            foreach (var contentReference in filteredContentReferences)
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

                    contentStore.RetrieveContent(contentReference, stream);

                    Console.WriteLine($"Restored: {file}");

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {file}{Environment.NewLine}{e}");

                    error = true;
                }
            }

            if (error)
            {
                throw new ChunkyardException(
                    "Detected errors while restoring snapshot");
            }
        }

        public static void ListSnapshots(ListOptions o)
        {
            var (repository, _, snapshotBuilder) = Create(o.Repository);
            var logPositions = repository
                .ListLogPositions()
                .ToArray();

            if (logPositions.Length == 0)
            {
                Console.WriteLine("Nothing to do");
                return;
            }

            foreach (var logPosition in logPositions)
            {
                var snapshot = snapshotBuilder.GetSnapshot(logPosition);

                Console.WriteLine($"{logPosition}: {snapshot.CreationTime}");
            }
        }

        public static void RemoveSnapshot(RemoveOptions o)
        {
            RemoveSnapshot(
                CreateRepository(o.Repository),
                o.LogPosition);
        }

        private static void RemoveSnapshot(
            IRepository repository,
            int logPosition)
        {
            repository.RemoveFromLog(logPosition);

            Console.WriteLine($"Removed snapshot: {logPosition}");
        }

        public static void KeepSnapshots(KeepOptions o)
        {
            var repository = CreateRepository(o.Repository);
            var logPositions = repository.ListLogPositions();

            var logPositionsToKeep = logPositions
                .TakeLast(o.LatestCount)
                .ToArray();

            var logPositionsToDelete = logPositions
                .Except(logPositionsToKeep)
                .ToArray();

            if (logPositionsToDelete.Length == 0)
            {
                Console.WriteLine("Nothing to do");
                return;
            }

            foreach (var logPosition in logPositionsToDelete)
            {
                RemoveSnapshot(repository, logPosition);
            }
        }

        public static void GarbageCollect(GarbageCollectOptions o)
        {
            var (repository, _, snapshotBuilder) = Create(o.Repository);
            var usedUris = new HashSet<Uri>();
            var allContentUris = repository.ListUris();
            var logPositions = repository.ListLogPositions();

            foreach (var logPosition in logPositions)
            {
                usedUris.UnionWith(
                    snapshotBuilder.ListUris(logPosition));
            }

            var contentUrisToDelete = allContentUris
                .Except(usedUris)
                .ToArray();

            if (contentUrisToDelete.Length == 0)
            {
                Console.WriteLine("Nothing to do");
                return;
            }

            foreach (var contentUri in allContentUris.Except(usedUris))
            {
                repository.RemoveValue(contentUri);

                Console.WriteLine($"Removed: {contentUri}");
            }
        }

        private static void PushSnapshots(
            string sourceRepositoryPath,
            string destinationRepositoryPath)
        {
            var (sourceRepository, _, snapshotBuilder) = Create(
                sourceRepositoryPath);

            var destinationRepository = new FileRepository(
                destinationRepositoryPath);

            var sourceLogs = sourceRepository
                .ListLogPositions()
                .ToArray();

            var destinationLogs = destinationRepository
                .ListLogPositions()
                .ToArray();

            if (sourceLogs.Length > 0 && destinationLogs.Length > 0)
            {
                var sourceRef = ContentStore.RetrieveFromLog(
                    sourceRepository,
                    sourceLogs[0]);

                var destinationRef = ContentStore.RetrieveFromLog(
                    destinationRepository,
                    destinationLogs[0]);

                if (sourceRef.LogId != destinationRef.LogId)
                {
                    throw new ChunkyardException(
                        "Cannot operate on repositories with different log IDs");
                }
            }

            var destinationMax = destinationLogs.Length == 0
                ? -1
                : destinationLogs.Max();

            var newLogPositions = sourceLogs
                .Where(l => l > destinationMax)
                .ToArray();

            if (newLogPositions.Length == 0)
            {
                Console.WriteLine("Nothing to do");
                return;
            }

            foreach (var logPosition in newLogPositions)
            {
                Console.WriteLine(
                    $"Transmitting snapshot: {logPosition}");

                PushSnapshot(
                    sourceRepository,
                    snapshotBuilder,
                    logPosition,
                    destinationRepository);
            }
        }

        private static void PushSnapshot(
            IRepository sourceRepository,
            SnapshotBuilder snapshotBuilder,
            int logPosition,
            IRepository destinationRepository)
        {
            var snapshotUris = snapshotBuilder
                .ListUris(logPosition)
                .ToArray();

            foreach (var snapshotUri in snapshotUris)
            {
                var contentValue = sourceRepository.RetrieveValue(snapshotUri);

                if (destinationRepository.ValueExists(snapshotUri))
                {
                    Console.WriteLine($"Exists: {snapshotUri}");
                }
                else
                {
                    destinationRepository.StoreValue(snapshotUri, contentValue);
                    Console.WriteLine($"Transmitted: {snapshotUri}");
                }
            }

            var logValue = sourceRepository.RetrieveFromLog(logPosition);

            destinationRepository.AppendToLog(logValue, logPosition);
        }

        private static IRepository CreateRepository(string repositoryPath)
        {
            return new FileRepository(repositoryPath);
        }

        private static (IRepository Repository, IContentStore ContentStore, SnapshotBuilder SnapshotBuilder) Create(
            string repositoryPath)
        {
            return Create(
                repositoryPath,
                false,
                new FastCdc(
                    FastCdc.DefaultMin,
                    FastCdc.DefaultAvg,
                    FastCdc.DefaultMax));
        }

        private static (IRepository Repository, IContentStore ContentStore, SnapshotBuilder SnapshotBuilder) Create(
            string repositoryPath,
            bool cached,
            FastCdc fastCdc)
        {
            var repository = CreateRepository(repositoryPath);
            var logPosition = repository.FetchLogPosition();
            var prompt = new EnvironmentPrompt(
                new ConsolePrompt());

            string? password;
            byte[]? salt;
            int? iterations;

            if (logPosition == null)
            {
                password = prompt.NewPassword();
                salt = AesGcmCrypto.GenerateSalt();
                iterations = AesGcmCrypto.Iterations;
            }
            else
            {
                var logReference = ContentStore.RetrieveFromLog(
                    repository,
                    logPosition.Value);

                password = prompt.ExistingPassword();
                salt = logReference.Salt;
                iterations = logReference.Iterations;
            }

            IContentStore contentStore = new ContentStore(
                repository,
                fastCdc,
                HashAlgorithmName.SHA256,
                password,
                salt,
                iterations.Value);

            if (cached)
            {
                // Each repository should have its own cache
                var shortHash = Id.ComputeHash(
                    HashAlgorithmName.SHA256,
                    repository.RepositoryUri.AbsoluteUri)
                    .Substring(0, 8);

                var cacheDirectory = Path.Combine(
                    CacheDirectoryPath,
                    shortHash);

                Console.WriteLine($"Using cache: {cacheDirectory}");

                contentStore = new CachedContentStore(
                    contentStore,
                    cacheDirectory);
            }

            var snapshotBuilder = new SnapshotBuilder(
                contentStore,
                logPosition);

            return (repository, contentStore, snapshotBuilder);
        }

        private static ContentReference[] FuzzyFilter(
            string fuzzyPattern,
            IEnumerable<ContentReference> contentReferences)
        {
            var fuzzy = new Fuzzy(fuzzyPattern);

            return contentReferences
                .Where(c => fuzzy.IsMatch(c.Name))
                .ToArray();
        }
    }
}
