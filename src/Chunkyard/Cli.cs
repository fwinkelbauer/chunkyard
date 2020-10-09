using System;
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

        private static readonly HashAlgorithmName DefaultAlgorithm =
            HashAlgorithmName.SHA256;

        public static void PreviewFiles(PreviewOptions o)
        {
            var files = FileFetcher.Find(o.Files, o.ExcludePatterns);

            foreach (var file in files)
            {
                Console.WriteLine(file.PartialPath);
            }
        }

        public static void CreateSnapshot(CreateOptions o)
        {
            var files = FileFetcher
                .Find(o.Files, o.ExcludePatterns)
                .ToArray();

            if (files.Length == 0)
            {
                Console.WriteLine("Empty file list");
                return;
            }

            var repository = CreateRepository(o.Repository);
            IContentStore contentStore = new PrintingContentStore(
                new ContentStore(
                    repository,
                    new FastCdc(o.Min, o.Avg, o.Max),
                    DefaultAlgorithm,
                    CreatePrompt()));

            if (o.Cached)
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

            var snapshotStore = new SnapshotStore(
                repository,
                contentStore);

            var logPosition = snapshotStore.AppendSnapshot(
                files.Select(f =>
                {
                    Func<Stream> openRead = () => File.OpenRead(f.AbsolutePath);
                    return (f.PartialPath, openRead);
                }),
                DateTime.Now);

            if (logPosition == null)
            {
                Console.WriteLine("No new data to store.");
                return;
            }

            var snapshotValid = snapshotStore.CheckSnapshotValid(
                logPosition.Value,
                "");

            if (!snapshotValid)
            {
                throw new ChunkyardException(
                    "Missign content after creating snapshot");
            }
        }

        public static void CheckSnapshot(CheckOptions o)
        {
            var snapshotStore = CreateSnapshotStore(o.Repository);

            var ok = o.Shallow
                ? snapshotStore.CheckSnapshotExists(
                    o.LogPosition,
                    o.IncludeFuzzy)
                : snapshotStore.CheckSnapshotValid(
                    o.LogPosition,
                    o.IncludeFuzzy);

            if (!ok)
            {
                throw new ChunkyardException(
                    "Found errors while checking snapshot");
            }
        }

        public static void ShowSnapshot(ShowOptions o)
        {
            var snapshotStore = CreateSnapshotStore(o.Repository);

            var contentReferences = snapshotStore.ShowSnapshot(
                o.LogPosition,
                o.IncludeFuzzy);

            foreach (var contentReference in contentReferences)
            {
                Console.WriteLine(contentReference.Name);
            }
        }

        public static void RestoreSnapshot(RestoreOptions o)
        {
            var snapshotStore = CreateSnapshotStore(o.Repository);

            Func<string, Stream> openWrite = (s) =>
            {
                var mode = o.Overwrite
                    ? FileMode.OpenOrCreate
                    : FileMode.CreateNew;

                var file = Path.Combine(o.Directory, s);

                Directory.CreateDirectory(Path.GetDirectoryName(file));

                return new FileStream(file, mode, FileAccess.Write);
            };

            var ok = snapshotStore.RestoreSnapshot(
                o.LogPosition,
                o.IncludeFuzzy,
                openWrite);

            if (!ok)
            {
                throw new ChunkyardException(
                    "Detected errors while restoring snapshot");
            }
        }

        public static void ListSnapshots(ListOptions o)
        {
            var snapshotStore = CreateSnapshotStore(o.Repository);

            var logPositions = snapshotStore.ListLogPositions()
                .ToArray();

            if (logPositions.Length == 0)
            {
                Console.WriteLine("Repository is empty");
                return;
            }

            foreach (var logPosition in logPositions)
            {
                var snapshot = snapshotStore.GetSnapshot(logPosition);
                var isoDate = snapshot.CreationTime.ToString(
                    "yyyy-MM-dd HH:mm:ss");

                Console.WriteLine($"{logPosition}: {isoDate}");
            }
        }

        public static void RemoveSnapshot(RemoveOptions o)
        {
            var repository = CreateRepository(o.Repository);

            repository.RemoveFromLog(o.LogPosition);
        }

        public static void KeepSnapshots(KeepOptions o)
        {
            CreateRepository(o.Repository)
                .KeepLatestLogPositions(o.LatestCount);
        }

        public static void GarbageCollect(GarbageCollectOptions o)
        {
            CreateSnapshotStore(o.Repository)
                .GarbageCollect();
        }

        public static void PushSnapshots(PushOptions o)
        {
            PushSnapshots(
                o.SourceRepository,
                o.DestinationRepository);
        }

        public static void PullSnapshots(PullOptions o)
        {
            PushSnapshots(
                o.SourceRepository,
                o.DestinationRepository);
        }

        private static void PushSnapshots(
            string sourceRepositoryPath,
            string destinationRepositoryPath)
        {
            var prompt = CreatePrompt();
            var source = CreateSnapshotStore(
                sourceRepositoryPath,
                prompt);

            var destination = CreateSnapshotStore(
                destinationRepositoryPath,
                prompt);

            var pushed = source.PushSnapshots(destination);

            if (!pushed)
            {
                Console.WriteLine("No new data to synchronize");
            }
        }

        private static IRepository CreateRepository(string repositoryPath)
        {
            return new PrintingRepository(
                new FileRepository(repositoryPath));
        }

        private static SnapshotStore CreateSnapshotStore(
            string repositoryPath,
            IPrompt? prompt = null)
        {
            var repository = CreateRepository(repositoryPath);

            return new SnapshotStore(
                repository,
                new PrintingContentStore(
                    new ContentStore(
                        repository,
                        new FastCdc(),
                        DefaultAlgorithm,
                        prompt ?? CreatePrompt())));
        }

        private static IPrompt CreatePrompt()
        {
            return new EnvironmentPrompt(
                new ConsolePrompt());
        }

        private class PrintingContentStore : DecoratorContentStore
        {
            public PrintingContentStore(IContentStore store)
                : base(store)
            {
            }

            public override (ContentReference ContentReference, bool IsNewContent) StoreContent(
                Stream inputStream,
                string contentName)
            {
                var result = base.StoreContent(inputStream, contentName);

                if (result.IsNewContent
                    && !contentName.Equals(SnapshotStore.SnapshotFile))
                {
                    Console.WriteLine($"Stored: {contentName}");
                }

                return result;
            }

            public override bool ContentExists(ContentReference contentReference)
            {
                var exists = base.ContentExists(contentReference);

                if (!exists)
                {
                    Console.WriteLine($"Missing: {contentReference.Name}");
                }

                return exists;
            }

            public override bool ContentValid(ContentReference contentReference)
            {
                var valid = base.ContentValid(contentReference);

                if (!valid)
                {
                    Console.WriteLine($"Invalid: {contentReference.Name}");
                }

                return valid;
            }

            public override void RetrieveContent(
                ContentReference contentReference,
                Stream outputStream)
            {
                try
                {
                    base.RetrieveContent(contentReference, outputStream);

                    if (!contentReference.Name.Equals(SnapshotStore.SnapshotFile))
                    {
                        Console.WriteLine($"Restored: {contentReference.Name}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        $"Error: {contentReference.Name}{Environment.NewLine}> {e.Message}");

                    throw;
                }
            }
        }

        private class PrintingRepository : DecoratorRepository
        {
            public PrintingRepository(IRepository repository)
                : base(repository)
            {
            }

            public override int AppendToLog(byte[] value, int newLogPosition)
            {
                var logPosition = base.AppendToLog(value, newLogPosition);

                Console.WriteLine($"Created: snapshot #{logPosition}");

                return logPosition;
            }

            public override void RemoveValue(Uri contentUri)
            {
                base.RemoveValue(contentUri);

                Console.WriteLine($"Removed: {contentUri}");
            }

            public override void RemoveFromLog(int logPosition)
            {
                base.RemoveFromLog(logPosition);

                Console.WriteLine($"Removed: snapshot #{logPosition}");
            }
        }
    }
}
