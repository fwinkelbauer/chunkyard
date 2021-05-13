﻿using System;
using System.IO;
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

            var snapshotStore = CreateSnapshotStore(o.Repository);

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
                    ? FileMode.Create
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

        private static SnapshotStore CreateSnapshotStore(
            string repositoryPath)
        {
            return new SnapshotStore(
                new ContentStore(
                    CreateUriRepository(repositoryPath),
                    new FastCdc(),
                    Id.AlgorithmSHA256),
                CreateIntRepository(repositoryPath),
                new EnvironmentPrompt(
                    new ConsolePrompt()),
                new ConsoleProbe());
        }

        private static IRepository<int> CreateIntRepository(
            string repositoryPath)
        {
            return FileRepository.CreateIntRepository(
                Path.Combine(repositoryPath, "snapshots"));
        }

        private static IRepository<Uri> CreateUriRepository(
            string repositoryPath)
        {
            return FileRepository.CreateUriRepository(
                Path.Combine(repositoryPath, "blobs"));
        }
    }
}
