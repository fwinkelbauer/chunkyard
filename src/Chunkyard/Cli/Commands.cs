using System;
using System.IO;
using System.Linq;
using Chunkyard.Core;
using Chunkyard.Infrastructure;

namespace Chunkyard.Cli
{
    /// <summary>
    /// Describes every available command line verb of the Chunkyard assembly.
    /// </summary>
    internal static class Commands
    {
        public static void PreviewSnapshot(PreviewOptions o)
        {
            var (_, blobs) = FileFetcher.FindBlobs(
                o.Files,
                new Fuzzy(o.ExcludePatterns, emptyMatches: false));

            if (blobs.Length == 0)
            {
                Console.WriteLine("Empty file list. Nothing to do!");
                return;
            }

            var snapshotStore = CreateSnapshotStore(o.Repository);

            var snapshotBlobs = snapshotStore.IsEmpty
                ? Array.Empty<Blob>()
                : snapshotStore.ShowSnapshot(
                    o.SnapshotId,
                    Fuzzy.MatchAll)
                    .Select(blobReference => blobReference.ToBlob())
                    .ToArray();

            var diff = DiffSet.Create(
                snapshotBlobs,
                blobs,
                blob => blob.Name,
                (b1, b2) => b1.Equals(b2));

            PrintDiff(diff);
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

            var snapshotStore = CreateSnapshotStore(o.Repository);

            snapshotStore.StoreSnapshot(
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

            if (!ok)
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
                    ? FileMode.Create
                    : FileMode.CreateNew;

                return new FileStream(file, mode, FileAccess.Write);
            }

            snapshotStore.RetrieveSnapshot(
                o.SnapshotId,
                new Fuzzy(o.IncludePatterns, emptyMatches: true),
                OpenWrite);
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

            var diff = DiffSet.Create(
                snapshotStore.GetSnapshot(o.FirstSnapshotId).BlobReferences,
                snapshotStore.GetSnapshot(o.SecondSnapshotId).BlobReferences,
                blobReference => blobReference.Name,
                (br1, br2) => o.ContentOnly
                    ? br1.ContentUris.SequenceEqual(br2.ContentUris)
                    : br1.Equals(br2));

            PrintDiff(diff);
        }

        public static void RemoveSnapshot(RemoveOptions o)
        {
            var snapshotStore = CreateSnapshotStore(o.Repository);

            snapshotStore.RemoveSnapshot(o.SnapshotId);
        }

        public static void KeepSnapshots(KeepOptions o)
        {
            var snapshotStore = CreateSnapshotStore(o.Repository);

            snapshotStore.KeepSnapshots(o.LatestCount);
        }

        public static void GarbageCollect(GarbageCollectOptions o)
        {
            var snapshotStore = CreateSnapshotStore(o.Repository);

            snapshotStore.GarbageCollect();
        }

        public static void Copy(CopyOptions o)
        {
            var snapshotStore = CreateSnapshotStore(o.SourceRepository);

            snapshotStore.Copy(
                CreateUriRepository(o.DestinationRepository),
                CreateIntRepository(o.DestinationRepository));
        }

        public static void Cat(CatOptions o)
        {
            var snapshotStore = CreateSnapshotStore(o.Repository);

            if (string.IsNullOrEmpty(o.Export))
            {
                using var stream = new MemoryStream();
                snapshotStore.RetrieveContent(o.ContentUris, stream);

                Console.WriteLine(
                    DataConvert.BytesToText(stream.ToArray()));
            }
            else
            {
                using var stream = new FileStream(
                    o.Export,
                    FileMode.CreateNew,
                    FileAccess.Write);

                snapshotStore.RetrieveContent(o.ContentUris, stream);
            }
        }

        private static void PrintDiff(DiffSet diff)
        {
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

        private static SnapshotStore CreateSnapshotStore(
            string repositoryPath)
        {
            return new SnapshotStore(
                CreateUriRepository(repositoryPath),
                CreateIntRepository(repositoryPath),
                new FastCdc(),
                Id.AlgorithmSHA256,
                new EnvironmentPrompt(
                    new ConsolePrompt()),
                new ConsoleProbe(),
                100);
        }

        private static IRepository<Uri> CreateUriRepository(
            string repositoryPath)
        {
            return FileRepository.CreateUriRepository(
                Path.Combine(repositoryPath, "blobs"));
        }

        private static IRepository<int> CreateIntRepository(
            string repositoryPath)
        {
            return FileRepository.CreateIntRepository(
                Path.Combine(repositoryPath, "snapshots"));
        }
    }
}
