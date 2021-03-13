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
    internal static class Commands
    {
        public const int LatestLogPosition = -1;

        private readonly static Dictionary<Uri, SnapshotStore> SnapshotStores =
            new Dictionary<Uri, SnapshotStore>();

        public static void PreviewFiles(PreviewOptions o)
        {
            var (_, blobs) = FileFetcher.FindBlobs(o.Files, o.ExcludePatterns);

            if (blobs.Length == 0)
            {
                Console.WriteLine("Empty file list. Nothing to do!");
                return;
            }

            foreach (var blob in blobs)
            {
                Console.WriteLine(blob.Name);
            }
        }

        public static void CreateSnapshot(CreateOptions o)
        {
            var (parent, blobs) = FileFetcher.FindBlobs(
                o.Files,
                o.ExcludePatterns);

            if (blobs.Length == 0)
            {
                Console.WriteLine("Empty file list. Nothing to do!");
                return;
            }

            var snapshotStore = CreateSnapshotStore(
                CreateContentStore(
                    CreateRepository(o.Repository, ensureRepository: false),
                    new FastCdc()),
                o.Cached);

            snapshotStore.AppendSnapshot(
                blobs,
                DateTime.Now,
                blobName => File.OpenRead(
                    Path.Combine(parent, blobName)));
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

            Stream OpenWrite(string blobName)
            {
                var file = Path.Combine(o.Directory, blobName);

                DirectoryUtil.CreateParent(file);

                var mode = o.Overwrite
                    ? FileMode.OpenOrCreate
                    : FileMode.CreateNew;

                return new FileStream(file, mode, FileAccess.Write);
            }

            snapshotStore.RestoreSnapshot(
                o.LogPosition,
                o.IncludeFuzzy,
                OpenWrite);
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
                    config.ExcludePatterns ?? Array.Empty<string>(),
                    config.Cached ?? false));

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

            sourceStore.CopySnapshots(destinationRepository);
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

            if (SnapshotStores.ContainsKey(repository.RepositoryUri))
            {
                return SnapshotStores[repository.RepositoryUri];
            }

            var snapshotStore = new SnapshotStore(
                contentStore,
                new EnvironmentPrompt(
                    new ConsolePrompt()),
                useCache);

            SnapshotStores[repository.RepositoryUri] = snapshotStore;

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
                    HashAlgorithmName.SHA256));
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

        private class PrintingContentStore : IContentStore
        {
            private readonly IContentStore _store;

            public PrintingContentStore(IContentStore store)
            {
                _store = store;
            }

            public IRepository Repository => _store.Repository;

            public void RetrieveBlob(
                BlobReference blobReference,
                byte[] key,
                Stream outputStream)
            {
                _store.RetrieveBlob(blobReference, key, outputStream);

                Console.WriteLine($"Restored blob: {blobReference.Name}");
            }

            public T RetrieveDocument<T>(
                DocumentReference documentReference,
                byte[] key)
                where T : notnull
            {
                return _store.RetrieveDocument<T>(documentReference, key);
            }

            public BlobReference StoreBlob(
                Blob blob,
                byte[] key,
                byte[] nonce,
                Stream inputStream)
            {
                var blobReference = _store.StoreBlob(blob, key, nonce, inputStream);

                Console.WriteLine($"Stored blob: {blobReference.Name}");

                return blobReference;
            }

            public DocumentReference StoreDocument<T>(
                T value,
                byte[] key,
                byte[] nonce)
                where T : notnull
            {
                return _store.StoreDocument(value, key, nonce);
            }

            public bool ContentExists(IContentReference contentReference)
            {
                var exists = _store.ContentExists(contentReference);

                if (!exists
                    && contentReference is BlobReference blobReference)
                {
                    Console.WriteLine(
                        $"Missing blob: {blobReference.Name}");
                }

                return exists;
            }

            public bool ContentValid(IContentReference contentReference)
            {
                var valid = _store.ContentValid(contentReference);

                if (!valid
                    && contentReference is BlobReference blobReference)
                {
                    Console.WriteLine(
                        $"Invalid blob: {blobReference.Name}");
                }

                return valid;
            }

            public void AppendToLog(
                int newLogPosition,
                LogReference logReference)
            {
                _store.AppendToLog(
                    newLogPosition,
                    logReference);
            }

            public LogReference RetrieveFromLog(int logPosition)
            {
                return _store.RetrieveFromLog(logPosition);
            }
        }

        private class PrintingRepository : IRepository
        {
            private readonly IRepository _repository;

            public PrintingRepository(IRepository repository)
            {
                _repository = repository;
            }

            public Uri RepositoryUri => _repository.RepositoryUri;

            public void StoreValue(Uri contentUri, byte[] value)
            {
                _repository.StoreValue(contentUri, value);
            }

            public byte[] RetrieveValue(Uri contentUri)
            {
                return _repository.RetrieveValue(contentUri);
            }

            public bool ValueExists(Uri contentUri)
            {
                return _repository.ValueExists(contentUri);
            }

            public Uri[] ListUris()
            {
                return _repository.ListUris();
            }

            public void RemoveValue(Uri contentUri)
            {
                _repository.RemoveValue(contentUri);

                Console.WriteLine($"Removed content: {contentUri}");
            }

            public void AppendToLog(int newLogPosition, byte[] value)
            {
                _repository.AppendToLog(newLogPosition, value);

                Console.WriteLine($"Created snapshot: #{newLogPosition}");
            }

            public byte[] RetrieveFromLog(int logPosition)
            {
                return _repository.RetrieveFromLog(logPosition);
            }

            public void RemoveFromLog(int logPosition)
            {
                _repository.RemoveFromLog(logPosition);

                Console.WriteLine($"Removed snapshot: #{logPosition}");
            }

            public int[] ListLogPositions()
            {
                return _repository.ListLogPositions();
            }
        }
    }
}
