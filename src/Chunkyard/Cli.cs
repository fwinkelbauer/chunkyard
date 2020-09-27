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
    internal class Cli
    {
        public const int LatestLogPosition = -1;

        private static readonly string CacheDirectoryPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "chunkyard",
            "cache");

        private readonly IRepository _repository;
        private readonly IContentStore _contentStore;
        private readonly SnapshotBuilder _snapshotBuilder;

        private Cli(string repositoryPath, FastCdc fastCdc, bool cached)
        {
            _repository = CreateRepository(repositoryPath);

            _contentStore = new ContentStore(
                _repository,
                fastCdc,
                HashAlgorithmName.SHA256,
                new EnvironmentPrompt(
                    new ConsolePrompt()));

            if (cached)
            {
                // Each repository should have its own cache
                var shortHash = Id.ComputeHash(
                    HashAlgorithmName.SHA256,
                    _repository.RepositoryUri.AbsoluteUri)
                    .Substring(0, 8);

                var cacheDirectory = Path.Combine(
                    CacheDirectoryPath,
                    shortHash);

                Console.WriteLine($"Using cache: {cacheDirectory}");

                _contentStore = new CachedContentStore(
                    _contentStore,
                    cacheDirectory);
            }

            _snapshotBuilder = new SnapshotBuilder(_contentStore);
        }

        private Cli(string repositoryPath)
            : this(
                repositoryPath,
                new FastCdc(FastCdc.DefaultMin, FastCdc.DefaultAvg, FastCdc.DefaultMax),
                false)
        {
        }

        public static void PreviewFiles(PreviewOptions o)
        {
            var foundTuples = FileFetcher.Find(o.Files, o.ExcludePatterns);

            foreach (var foundTuple in foundTuples)
            {
                Console.WriteLine(foundTuple.ContentName);
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
            var cli = new Cli(
                o.Repository,
                new FastCdc(o.Min, o.Avg, o.Max),
                o.Cached);

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
                var newContent = cli._snapshotBuilder.AddContent(
                    fileStream,
                    foundTuple.ContentName);

                if (newContent)
                {
                    Console.WriteLine($"Stored: {foundTuple.ContentName}");
                }
            }

            var newLogPosition = cli._snapshotBuilder.WriteSnapshot(
                DateTime.Now);

            var newSnapshot = cli._snapshotBuilder.GetSnapshot(newLogPosition);

            // Perform a shallow check to make sure that our new snapshot is
            // alright
            foreach (var contentReference in newSnapshot.ContentReferences)
            {
                if (!cli._contentStore.ContentExists(contentReference))
                {
                    throw new ChunkyardException(
                        $"Missing content {contentReference.Name} after creating snapshot");
                }
            }

            Console.WriteLine($"Created snapshot: {newLogPosition}");
        }

        public static void CheckSnapshot(CheckOptions o)
        {
            var cli = new Cli(o.Repository);

            var snapshot = cli._snapshotBuilder.GetSnapshot(o.LogPosition);
            var filteredContentReferences = FuzzyFilter(
                o.IncludeFuzzy,
                snapshot.ContentReferences);

            var error = false;

            foreach (var contentReference in filteredContentReferences)
            {
                if (!cli._contentStore.ContentExists(contentReference))
                {
                    Console.WriteLine($"Missing: {contentReference.Name}");

                    error = true;
                }
                else if (!o.Shallow
                    && !cli._contentStore.ContentValid(contentReference))
                {
                    Console.WriteLine($"Corrupted: {contentReference.Name}");

                    error = true;
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
            var cli = new Cli(o.Repository);

            var snapshot = cli._snapshotBuilder.GetSnapshot(o.LogPosition);
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
            var cli = new Cli(o.Repository);

            var snapshot = cli._snapshotBuilder.GetSnapshot(o.LogPosition);
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

                    cli._contentStore.RetrieveContent(contentReference, stream);

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
            var cli = new Cli(o.Repository);
            var logPositions = cli._repository
                .ListLogPositions()
                .ToArray();

            if (logPositions.Length == 0)
            {
                Console.WriteLine("Nothing to do");
                return;
            }

            foreach (var logPosition in logPositions)
            {
                var snapshot = cli._snapshotBuilder.GetSnapshot(logPosition);

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
            var logPositions = repository.ListLogPositions()
                .ToArray();

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
            var cli = new Cli(o.Repository);
            var usedUris = new HashSet<Uri>();
            var allContentUris = cli._repository.ListUris()
                .ToArray();

            var logPositions = cli._repository.ListLogPositions();

            foreach (var logPosition in logPositions)
            {
                usedUris.UnionWith(
                    cli._snapshotBuilder.ListUris(logPosition));
            }

            var contentUrisToDelete = allContentUris
                .Except(usedUris)
                .ToArray();

            if (contentUrisToDelete.Length == 0)
            {
                Console.WriteLine("Nothing to do");
                return;
            }

            var removed = 0;

            foreach (var contentUri in allContentUris.Except(usedUris))
            {
                cli._repository.RemoveValue(contentUri);
                removed++;
            }

            if (removed > 0)
            {
                var word = removed == 1
                    ? "chunk"
                    : "chunks";

                Console.WriteLine($"Removed {removed} {word}");
            }
        }

        private static void PushSnapshots(
            string sourceRepositoryPath,
            string destinationRepositoryPath)
        {
            var sourceCli = new Cli(sourceRepositoryPath);
            var destinationCli = new Cli(destinationRepositoryPath);

            var sourceLogs = sourceCli._repository
                .ListLogPositions()
                .ToArray();

            var destinationLogs = destinationCli._repository
                .ListLogPositions()
                .ToArray();

            if (sourceLogs.Length > 0 && destinationLogs.Length > 0)
            {
                var sourceRef = sourceCli._contentStore.RetrieveFromLog(
                    sourceLogs[0]);

                var destinationRef = destinationCli._contentStore.RetrieveFromLog(
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
                    sourceCli._repository,
                    sourceCli._snapshotBuilder,
                    logPosition,
                    destinationCli._repository);
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
                if (!destinationRepository.ValueExists(snapshotUri))
                {
                    var contentValue = sourceRepository.RetrieveValue(snapshotUri);
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
