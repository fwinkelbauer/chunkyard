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

        public static void CreateSnapshot(CreateOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(o.Repository, o.Cached);

            Console.WriteLine("Creating new snapshot");

            var foundTuples = FileFetcher.Find(o.Files, o.ExcludePatterns)
                .ToList();

            Parallel.ForEach(
                foundTuples,
                t =>
                {
                    using var fileStream = File.OpenRead(t.FoundFile);
                    snapshotBuilder.AddContent(fileStream, t.ContentName);

                    Console.WriteLine($"Stored: {t.ContentName}");
                });

            var newLogPosition = snapshotBuilder.WriteSnapshot(DateTime.Now);

            Console.WriteLine($"Latest snapshot is {newLogPosition}");
        }

        public static void CheckSnapshot(CheckOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(o.Repository);
            var logPosition = snapshotBuilder.ResolveLogPosition(o.LogPosition);

            Console.WriteLine($"Checking snapshot {logPosition}");

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

        public static void ListSnapshot(ListOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(o.Repository);
            var logPosition = snapshotBuilder.ResolveLogPosition(o.LogPosition);

            Console.WriteLine($"Listing snapshot {logPosition}");

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

            Console.WriteLine($"Restoring snapshot {logPosition}");

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

        public static void ShowLogPositions(LogOptions o)
        {
            var snapshotBuilder = CreateSnapshotBuilder(o.Repository);
            var logPositions = snapshotBuilder.ContentStore.ListLogPositions();

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

            Console.WriteLine($"Removing snapshot: {logPosition}");
            snapshotBuilder.ContentStore.RemoveFromLog(logPosition);
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
                var contentUris = snapshotBuilder.ListUris(logPosition);

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

                Console.WriteLine($"Unused: {contentUri}");

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
                new FastCdc(
                    4 * 1024 * 1024,
                    8 * 1024 * 1024,
                    16 * 1024 * 1024),
                HashAlgorithmName.SHA256,
                password,
                salt,
                iterations.Value);

            if (cached)
            {
                contentStore = new CachedContentStore(
                    contentStore,
                    CacheDirectoryPath);
            }

            return new SnapshotBuilder(
                contentStore,
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
