using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Chunkyard.Core;
using Chunkyard.Infrastructure;

namespace Chunkyard.Cli
{
    /// <summary>
    /// Describes every available command line verb of the Chunkyard assembly.
    /// </summary>
    internal class Commands
    {
        public const int LatestLogPosition = -1;

        private static readonly string CacheDirectoryPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "chunkyard",
            "cache");

        private static readonly HashAlgorithmName DefaultAlgorithm =
            HashAlgorithmName.SHA256;

        private static Dictionary<Uri, SnapshotStore> _snapshotStores =
            new Dictionary<Uri, SnapshotStore>();

        public static void PreviewFiles(PreviewOptions o)
        {
            var files = FileFetcher.Find(o.Files, o.ExcludePatterns);

            if (files.Length == 0)
            {
                Console.WriteLine("Empty file list. Nothing to do!");
                return;
            }

            foreach (var file in files)
            {
                Console.WriteLine(file);
            }
        }

        public static void CreateSnapshot(CreateOptions o)
        {
            var files = FileFetcher.Find(o.Files, o.ExcludePatterns);
            var parent = FileFetcher.FindCommonParent(files);

            if (files.Length == 0)
            {
                Console.WriteLine("Empty file list. Nothing to do!");
                return;
            }

            var repository = CreateRepository(
                o.Repository,
                ensureRepository: false);

            var contentStore = CreateContentStore(
                repository,
                new FastCdc(o.Min, o.Avg, o.Max));

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

            var snapshotStore = CreateSnapshotStore(contentStore);

            var contentNames = FileFetcher.ToContentNames(parent, files);

            Stream openRead(string subPath)
            {
                return File.OpenRead(
                    Path.Combine(parent, subPath));
            }

            snapshotStore.AppendSnapshot(
                contentNames,
                openRead,
                DateTime.Now);
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

            Stream openWrite(string s)
            {
                var mode = o.Overwrite
                    ? FileMode.OpenOrCreate
                    : FileMode.CreateNew;

                var file = Path.Combine(o.Directory, s);

                DirectoryUtil.CreateParent(file);

                return new FileStream(file, mode, FileAccess.Write);
            }

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

                Console.WriteLine($"Snapshot #{logPosition}: {isoDate}");
            }
        }

        public static void RemoveSnapshot(RemoveOptions o)
        {
            CreateRepository(o.Repository)
                .RemoveFromLog(o.LogPosition);
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

        public static void Dot(DotOptions o)
        {
            static string? FindFile(string file)
            {
                return File.Exists(file)
                    ? file
                    : null;
            }

            var file = o.File
                ?? FindFile(".config/chunkyard.json")
                ?? ".chunkyard";

            var config = DataConvert.ToObject<DotConfig>(
                File.ReadAllBytes(file));

            CreateSnapshot(
                new CreateOptions(
                    config.Repository,
                    config.Files,
                    (IEnumerable<string>?)config.ExcludePatterns ?? new List<string>(),
                    config.Cached ?? false,
                    config.Min ?? FastCdc.DefaultMin,
                    config.Avg ?? FastCdc.DefaultAvg,
                    config.Max ?? FastCdc.DefaultMax));

            if (config.LatestCount.HasValue)
            {
                KeepSnapshots(
                    new KeepOptions(
                        config.Repository,
                        config.LatestCount.Value));

                GarbageCollect(
                    new GarbageCollectOptions(
                        config.Repository));
            }

            CheckSnapshot(
                new CheckOptions(
                    config.Repository,
                    LatestLogPosition,
                    "",
                    false));
        }

        public static void CopySnapshots(CopyOptions o)
        {
            var sourceStore = CreateSnapshotStore(o.SourceRepository);
            var destinationRepository = CreateRepository(
                o.DestinationRepository,
                ensureRepository: false);

            if (!sourceStore.CopySnapshots(destinationRepository).Any())
            {
                Console.WriteLine("No new snapshots to copy");
            }
        }

        private static SnapshotStore CreateSnapshotStore(
            string repositoryPath,
            bool ensureRepository = true)
        {
            return CreateSnapshotStore(
                CreateRepository(repositoryPath, ensureRepository));
        }

        private static SnapshotStore CreateSnapshotStore(
            IRepository repository,
            FastCdc? fastCdc = null)
        {
            return CreateSnapshotStore(
                CreateContentStore(repository, fastCdc));
        }

        private static SnapshotStore CreateSnapshotStore(
            IContentStore contentStore)
        {
            var repository = contentStore.Repository;

            if (_snapshotStores.ContainsKey(repository.RepositoryUri))
            {
                return _snapshotStores[repository.RepositoryUri];
            }

            var snapshotStore = new SnapshotStore(
                contentStore,
                new EnvironmentPrompt(
                    new ConsolePrompt()));

            _snapshotStores[repository.RepositoryUri] = snapshotStore;

            return snapshotStore;
        }

        private static IContentStore CreateContentStore(
            IRepository repository,
            FastCdc? fastCdc = null)
        {
            return new PrintingContentStore(
                new ContentStore(
                    repository,
                    fastCdc ?? new FastCdc(),
                    DefaultAlgorithm));
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

        private class PrintingContentStore : DecoratorContentStore
        {
            public PrintingContentStore(IContentStore store)
                : base(store)
            {
            }

            public override ContentReference StoreContent(
                Stream inputStream,
                string contentName,
                byte[] key,
                byte[] nonce,
                ContentType type,
                out bool isNewContent)
            {
                var contentReference = base.StoreContent(
                    inputStream,
                    contentName,
                    key,
                    nonce,
                    type,
                    out isNewContent);

                if (contentReference.Type == ContentType.Blob
                    && isNewContent)
                {
                    Console.WriteLine($"Stored content: {contentName}");
                }

                return contentReference;
            }

            public override bool ContentExists(
                ContentReference contentReference)
            {
                var exists = base.ContentExists(contentReference);

                if (!exists)
                {
                    Console.WriteLine($"Missing content: {contentReference.Name}");
                }

                return exists;
            }

            public override bool ContentValid(ContentReference contentReference)
            {
                var valid = base.ContentValid(contentReference);

                if (!valid)
                {
                    Console.WriteLine($"Invalid content: {contentReference.Name}");
                }

                return valid;
            }

            public override void RetrieveContent(
                ContentReference contentReference,
                byte[] key,
                Stream outputStream)
            {
                base.RetrieveContent(contentReference, key, outputStream);

                if (contentReference.Type == ContentType.Blob)
                {
                    Console.WriteLine($"Restored content: {contentReference.Name}");
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

                Console.WriteLine($"Created snapshot: #{logPosition}");

                return logPosition;
            }

            public override void RemoveValue(Uri contentUri)
            {
                base.RemoveValue(contentUri);

                Console.WriteLine($"Removed content: {contentUri}");
            }

            public override void RemoveFromLog(int logPosition)
            {
                base.RemoveFromLog(logPosition);

                Console.WriteLine($"Removed snapshot: #{logPosition}");
            }
        }
    }
}
