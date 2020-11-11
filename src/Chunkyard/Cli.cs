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
                Console.WriteLine(file.ContentName);
            }
        }

        public static void CreateSnapshot(CreateOptions o)
        {
            var files = FileFetcher.Find(o.Files, o.ExcludePatterns)
                .ToArray();

            if (files.Length == 0)
            {
                Console.WriteLine("Empty file list");
                return;
            }

            var repository = CreateRepository(
                o.Repository,
                ensureRepository: false);

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

            var snapshotStore = new SnapshotStore(contentStore);

            var logPosition = snapshotStore.AppendSnapshot(
                files.Select(f =>
                {
                    Func<Stream> openRead = () => File.OpenRead(f.AbsolutePath);
                    return (f.ContentName, openRead);
                }),
                DateTime.Now);

            var snapshotExists = snapshotStore.CheckSnapshotExists(logPosition);

            if (!snapshotExists)
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

            if (ok)
            {
                Console.WriteLine("Snapshot is valid");
            }
            else
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

                DirectoryUtil.CreateParent(file);

                return new FileStream(file, mode, FileAccess.Write);
            };

            snapshotStore.RestoreSnapshot(
                o.LogPosition,
                o.IncludeFuzzy,
                openWrite);
        }

        public static void ListSnapshots(ListOptions o)
        {
            var repository = CreateRepository(o.Repository);
            var snapshotStore = CreateSnapshotStore(repository);

            foreach (var logPosition in repository.ListLogPositions())
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

        public static void CopySnapshots(CopyOptions o)
        {
            var prompt = CreatePrompt();
            var source = CreateSnapshotStore(
                o.SourceRepository,
                prompt);

            var destination = CreateSnapshotStore(
                o.DestinationRepository,
                prompt,
                ensureRepository: false);

            if (!source.CopySnapshots(destination).Any())
            {
                Console.WriteLine("No new snapshots to copy");
            }
        }

        private static IRepository CreateRepository(
            string repositoryPath,
            bool ensureRepository = true)
        {
            var repository = new PrintingRepository(
                new FileRepository(repositoryPath));

            if (ensureRepository
                && !repository.ListLogPositions().Any())
            {
                throw new ChunkyardException(
                    "Cannot perform command on an empty repository");
            }

            return repository;
        }

        private static SnapshotStore CreateSnapshotStore(
            string repositoryPath,
            IPrompt? prompt = null,
            bool ensureRepository = true)
        {
            return CreateSnapshotStore(
                CreateRepository(repositoryPath, ensureRepository),
                prompt);
        }

        private static SnapshotStore CreateSnapshotStore(
            IRepository repository,
            IPrompt? prompt = null)
        {
            return new SnapshotStore(
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

            public override ContentReference StoreContent(
                Stream inputStream,
                string contentName,
                byte[] nonce,
                ContentType type,
                out bool newContent)
            {
                var contentReference = base.StoreContent(
                    inputStream,
                    contentName,
                    nonce,
                    type,
                    out newContent);

                if (contentReference.Type == ContentType.Blob
                    && newContent)
                {
                    Console.WriteLine($"Stored: {contentName}");
                }

                return contentReference;
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
                base.RetrieveContent(contentReference, outputStream);

                if (contentReference.Type == ContentType.Blob)
                {
                    Console.WriteLine($"Restored: {contentReference.Name}");
                }
            }
        }

        private class PrintingRepository : DecoratorRepository
        {
            public PrintingRepository(IRepository repository)
                : base(repository)
            {
            }

            public override int AppendToLog(int newLogPosition, byte[] value)
            {
                var logPosition = base.AppendToLog(newLogPosition, value);

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
