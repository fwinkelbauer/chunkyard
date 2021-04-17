using System;
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
        public static void PreviewFiles(PreviewOptions o)
        {
            var (_, blobs) = FileFetcher.FindBlobs(
                o.Files,
                new Fuzzy(o.ExcludePatterns, emptyMatches: false));

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
                new Fuzzy(o.ExcludePatterns, emptyMatches: false));

            if (blobs.Length == 0)
            {
                Console.WriteLine("Empty file list. Nothing to do!");
                return;
            }

            var snapshotStore = CreateSnapshotStore(
                o.Repository,
                ensureRepository: false);

            snapshotStore.AppendSnapshot(
                blobs,
                new Fuzzy(o.ScanPatterns, emptyMatches: false),
                DateTime.UtcNow,
                blobName => File.OpenRead(
                    Path.Combine(parent, blobName)));
        }

        public static void CheckSnapshot(CheckOptions o)
        {
            var snapshotStore = CreateSnapshotStore(o.Repository);
            var fuzzy = new Fuzzy(o.IncludePatterns, emptyMatches: true);

            var ok = o.Shallow
                ? snapshotStore.CheckSnapshotExists(
                    o.SnapshotId,
                    fuzzy)
                : snapshotStore.CheckSnapshotValid(
                    o.SnapshotId,
                    fuzzy);

            if (ok)
            {
                Console.WriteLine(o.Shallow
                    ? "Snapshot is complete"
                    : "Snapshot is valid");
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
                o.SnapshotId,
                new Fuzzy(o.IncludePatterns, emptyMatches: true));

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

            var restoredBlobs = snapshotStore.RestoreSnapshot(
                o.SnapshotId,
                new Fuzzy(o.IncludePatterns, emptyMatches: true),
                OpenWrite);

            foreach (var blob in restoredBlobs)
            {
                var path = Path.Combine(o.Directory, blob.Name);

                File.SetCreationTimeUtc(path, blob.CreationTimeUtc);
                File.SetLastWriteTimeUtc(path, blob.LastWriteTimeUtc);
            }
        }

        public static void ListSnapshots(ListOptions o)
        {
            var snapshotStore = CreateSnapshotStore(o.Repository);

            foreach (var snapshot in snapshotStore.GetSnapshots())
            {
                var isoDate = snapshot.CreationTimeUtc
                    .ToLocalTime()
                    .ToString("yyyy-MM-dd HH:mm:ss");

                Console.WriteLine(
                    $"Snapshot #{snapshot.SnapshotId}: {isoDate}");
            }
        }

        public static void DiffSnapshots(DiffOptions o)
        {
            var snapshotStore = CreateSnapshotStore(o.Repository);

            var diff = Snapshot.Diff(
                snapshotStore.GetSnapshot(o.FirstSnapshotId),
                snapshotStore.GetSnapshot(o.SecondSnapshotId));

            foreach (var added in diff.Added)
            {
                Console.WriteLine($"+ {added}");
            }

            foreach (var changed in diff.Changed)
            {
                Console.WriteLine($"~ {changed}");
            }

            foreach (var removed in diff.Removed)
            {
                Console.WriteLine($"- {removed}");
            }
        }

        public static void RemoveSnapshot(RemoveOptions o)
        {
            CreateIntRepository(o.Repository)
                .RemoveValue(o.SnapshotId);
        }

        public static void KeepSnapshots(KeepOptions o)
        {
            CreateIntRepository(o.Repository)
                .KeepLatestValues(o.LatestCount);
        }

        public static void GarbageCollect(GarbageCollectOptions o)
        {
            CreateSnapshotStore(o.Repository)
                .GarbageCollect();
        }

        public static void Copy(CopyOptions o)
        {
            CreateUriRepository(o.SourceRepository)
                .Copy(CreateUriRepository(o.DestinationRepository));

            CreateIntRepository(o.SourceRepository)
                .Copy(CreateIntRepository(
                    o.DestinationRepository,
                    ensureRepository: false));
        }

        private static SnapshotStore CreateSnapshotStore(
            string repositoryPath,
            bool ensureRepository = true)
        {
            return new SnapshotStore(
                new PrintingContentStore(
                    new ContentStore(
                        CreateUriRepository(repositoryPath),
                        new FastCdc(),
                        HashAlgorithmName.SHA256)),
                CreateIntRepository(repositoryPath, ensureRepository),
                new EnvironmentPrompt(
                    new ConsolePrompt()));
        }

        private static IRepository<int> CreateIntRepository(
            string repositoryPath,
            bool ensureRepository = true)
        {
            var repository = new PrintingRepository(
                FileRepository<int>.CreateIntRepository(repositoryPath));

            if (ensureRepository
                && !repository.ListKeys().Any())
            {
                throw new ChunkyardException(
                    "Cannot perform command on an empty repository");
            }

            return repository;
        }

        private static IRepository<Uri> CreateUriRepository(
            string repositoryPath)
        {
            return FileRepository<int>.CreateUriRepository(
                repositoryPath);
        }

        private class PrintingContentStore : IContentStore
        {
            private readonly IContentStore _store;

            public PrintingContentStore(IContentStore store)
            {
                _store = store;
            }

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

                if (contentReference is BlobReference blobReference)
                {
                    Console.WriteLine(exists
                        ? $"Existing blob: {blobReference.Name}"
                        : $"Missing blob: {blobReference.Name}");
                }

                return exists;
            }

            public bool ContentValid(IContentReference contentReference)
            {
                var valid = _store.ContentValid(contentReference);

                if (contentReference is BlobReference blobReference)
                {
                    Console.WriteLine(valid
                        ? $"Valid blob: {blobReference.Name}"
                        : $"Invalid blob: {blobReference.Name}");
                }

                return valid;
            }

            public Uri[] ListContentUris()
            {
                return _store.ListContentUris();
            }

            public void RemoveContent(Uri contentUri)
            {
                _store.RemoveContent(contentUri);

                Console.WriteLine($"Removed content: {contentUri}");
            }
        }

        private class PrintingRepository : IRepository<int>
        {
            private readonly IRepository<int> _repository;

            public PrintingRepository(IRepository<int> repository)
            {
                _repository = repository;
            }

            public void StoreValue(int key, byte[] value)
            {
                _repository.StoreValue(key, value);

                Console.WriteLine($"Created snapshot: #{key}");
            }

            public byte[] RetrieveValue(int key)
            {
                return _repository.RetrieveValue(key);
            }

            public bool ValueExists(int key)
            {
                return _repository.ValueExists(key);
            }

            public int[] ListKeys()
            {
                return _repository.ListKeys();
            }

            public void RemoveValue(int key)
            {
                _repository.RemoveValue(key);

                Console.WriteLine($"Removed snapshot: #{key}");
            }
        }
    }
}
