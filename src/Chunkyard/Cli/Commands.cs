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

            var snapshotStore = CreateSnapshotStore(
                CreateContentStore(
                    CreateRepository(o.Repository, ensureRepository: false),
                    new FastCdc(o.Min, o.Avg, o.Max)),
                o.Cached);

            var blobs = FileFetcher.FetchBlobs(parent, files);

            snapshotStore.AppendSnapshot(
                blobs,
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

            var blobReferences = snapshotStore.ShowSnapshot(
                o.LogPosition,
                o.IncludeFuzzy);

            foreach (var blobReference in blobReferences)
            {
                Console.WriteLine(blobReference.Name);
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
                    true));
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
            IContentStore contentStore,
            bool useCache = false)
        {
            var repository = contentStore.Repository;

            if (_snapshotStores.ContainsKey(repository.RepositoryUri))
            {
                return _snapshotStores[repository.RepositoryUri];
            }

            var snapshotStore = new SnapshotStore(
                contentStore,
                new EnvironmentPrompt(
                    new ConsolePrompt()),
                useCache);

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

            public override void RetrieveBlob(
                BlobReference blobReference,
                byte[] key,
                Stream outputStream)
            {
                base.RetrieveBlob(blobReference, key, outputStream);

                Console.WriteLine($"Restored blob: {blobReference.Name}");
            }

            public override BlobReference StoreBlob(
                Blob blob,
                byte[] key,
                byte[] nonce)
            {
                var blobReference = base.StoreBlob(blob, key, nonce);

                Console.WriteLine($"Stored blob: {blobReference.Name}");

                return blobReference;
            }

            public override bool ContentExists(
                IContentReference contentReference)
            {
                var exists = base.ContentExists(contentReference);

                if (!exists
                    && contentReference is BlobReference blobReference)
                {
                    Console.WriteLine(
                        $"Missing blob: {blobReference.Name}");
                }

                return exists;
            }

            public override bool ContentValid(
                IContentReference contentReference)
            {
                var valid = base.ContentValid(contentReference);

                if (!valid
                    && contentReference is BlobReference blobReference)
                {
                    Console.WriteLine(
                        $"Invalid blob: {blobReference.Name}");
                }

                return valid;
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
