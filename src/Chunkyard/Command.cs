using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
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
            var snapshotBuilder = CreateSnapshotBuilder(
                o.Repository,
                o.Cached,
                new FastCdc(o.Min, o.Avg, o.Max));

            Console.WriteLine("Creating new snapshot");

            var foundTuples = FileFetcher.Find(o.Files, o.ExcludePatterns)
                .ToArray();

            Parallel.ForEach(
                foundTuples,
                t =>
                {
                    using var fileStream = File.OpenRead(t.FoundFile);
                    snapshotBuilder.AddContent(fileStream, t.ContentName);

                    Console.WriteLine($"Stored: {t.ContentName}");
                });

            var newLogPosition = snapshotBuilder.WriteSnapshot(DateTime.Now);

            Console.WriteLine($"Latest snapshot: {newLogPosition}");
        }

        public static void CheckSnapshot(CheckOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(o.Repository);
            var logPosition = snapshotBuilder.ResolveLogPosition(o.LogPosition);

            Console.WriteLine($"Checking snapshot: {logPosition}");

            var snapshot = snapshotBuilder.GetSnapshot(logPosition);
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
                        Console.WriteLine($"Missing: {contentReference.Name}");

                        error = true;
                    }
                    else if (!o.Shallow
                        && !snapshotBuilder.ContentStore
                             .ContentValid(contentReference))
                    {
                        Console.WriteLine(
                            $"Corrupted: {contentReference.Name}");

                        error = true;
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Validated: {contentReference.Name}");
                    }
                });

            if (error)
            {
                throw new ChunkyardException(
                    "Detected errors while verifying snapshot");
            }
        }

        public static void ShowSnapshot(ShowOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(o.Repository);
            var logPosition = snapshotBuilder.ResolveLogPosition(o.LogPosition);

            Console.WriteLine($"Listing snapshot: {logPosition}");

            var snapshot = snapshotBuilder.GetSnapshot(logPosition);
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
            var snapshotBuilder = CreateSnapshotBuilder(o.Repository);
            var logPosition = snapshotBuilder.ResolveLogPosition(o.LogPosition);

            Console.WriteLine($"Restoring snapshot: {logPosition}");

            var snapshot = snapshotBuilder.GetSnapshot(logPosition);
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

                        snapshotBuilder.ContentStore.
                            RetrieveContent(contentReference, stream);

                        Console.WriteLine($"Restored: {file}");

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error: {file}");
                        Console.WriteLine(e);

                        error = true;
                    }
                });

            if (error)
            {
                throw new ChunkyardException(
                    "Detected errors while restoring snapshot");
            }
        }

        public static void ListSnapshots(ListOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(o.Repository);
            var logPositions = snapshotBuilder.ContentStore.Repository
                .ListLogPositions();

            foreach (var logPosition in logPositions)
            {
                var snapshot = snapshotBuilder.GetSnapshot(logPosition);

                Console.WriteLine($"{logPosition}: {snapshot.CreationTime}");
            }
        }

        public static void RemoveSnapshot(RemoveOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(o.Repository);
            var logPosition = snapshotBuilder.ResolveLogPosition(o.LogPosition);

            RemoveSnapshot(snapshotBuilder, logPosition);
        }

        private static void RemoveSnapshot(
            SnapshotBuilder snapshotBuilder,
            int logPosition)
        {
            Console.WriteLine($"Removing snapshot: {logPosition}");

            snapshotBuilder.ContentStore.Repository
                .RemoveFromLog(logPosition);
        }

        public static void KeepSnapshots(KeepOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(o.Repository);
            var logPositions = snapshotBuilder.ContentStore.Repository
                .ListLogPositions();

            var logPositionsToKeep = o.LogPositions
                .Select(l => snapshotBuilder.ResolveLogPosition(l));

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
                RemoveSnapshot(snapshotBuilder, logPosition);
            }
        }

        public static void GarbageCollect(GarbageCollectOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(o.Repository);
            var usedUris = new HashSet<Uri>();
            var allContentUris = snapshotBuilder.ContentStore.Repository
                .ListUris();

            var logPositions = snapshotBuilder.ContentStore.Repository
                .ListLogPositions();

            foreach (var logPosition in logPositions)
            {
                usedUris.UnionWith(
                    snapshotBuilder.ListUris(logPosition));
            }

            foreach (var contentUri in allContentUris.Except(usedUris))
            {
                if (o.Preview)
                {
                    Console.WriteLine($"Unused: {contentUri}");
                }
                else
                {
                    snapshotBuilder.ContentStore.Repository
                        .RemoveUri(contentUri);

                    Console.WriteLine($"Removed: {contentUri}");
                }
            }
        }

        private static void PushSnapshots(
            string sourceRepositoryPath,
            string destinationRepositoryPath)
        {
            var snapshotBuilder = CreateSnapshotBuilder(sourceRepositoryPath);
            var destinationRepository = new FileRepository(
                destinationRepositoryPath);

            var sourceLogs = snapshotBuilder.ContentStore.Repository
                .ListLogPositions()
                .ToArray();

            var destinationLogs = destinationRepository
                .ListLogPositions()
                .ToArray();

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
                    $"Ttransmitting snapshot: {logPosition}");

                PushSnapshot(
                    snapshotBuilder,
                    logPosition,
                    destinationRepository);
            }
        }

        private static void PushSnapshot(
            SnapshotBuilder snapshotBuilder,
            int logPosition,
            IRepository destinationRepository)
        {
            var snapshotUris = snapshotBuilder.ListUris(logPosition)
                .ToArray();

            Parallel.ForEach(
                snapshotUris,
                u =>
                {
                    var contentValue = snapshotBuilder.ContentStore.Repository
                        .RetrieveUri(u);

                    if (destinationRepository.UriExists(u))
                    {
                        Console.WriteLine($"Exists: {u}");
                    }
                    else
                    {
                        destinationRepository.StoreUri(u, contentValue);
                        Console.WriteLine($"Transmitted: {u}");
                    }
                });

            var logValue = snapshotBuilder.ContentStore.Repository
                .RetrieveFromLog(logPosition);

            destinationRepository.AppendToLog(logValue, logPosition);
        }

        private static SnapshotBuilder CreateSnapshotBuilder(
            string repositoryPath)
        {
            return CreateSnapshotBuilder(
                repositoryPath,
                false,
                new FastCdc(
                    4 * 1024 * 1024,
                    8 * 1024 * 1024,
                    16 * 1024 * 1024));
        }

        private static SnapshotBuilder CreateSnapshotBuilder(
            string repositoryPath,
            bool cached,
            FastCdc fastCdc)
        {
            var repository = new FileRepository(repositoryPath);
            var logPosition = repository.FetchLogPosition();
            var prompt = new EnvironmentPrompt(
                new ConsolePrompt());

            string? password = null;
            byte[]? salt = null;
            int? iterations = null;

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
                    contentStore.Repository.RepositoryUri.AbsoluteUri)
                    .Substring(0, 8);

                var cacheDirectory = Path.Combine(
                    CacheDirectoryPath,
                    shortHash);

                Console.WriteLine($"Using cache: {cacheDirectory}");

                contentStore = new CachedContentStore(
                    contentStore,
                    cacheDirectory);
            }

            return new SnapshotBuilder(
                contentStore,
                logPosition);
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
