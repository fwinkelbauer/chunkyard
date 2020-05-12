using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Chunkyard.Options;
using Newtonsoft.Json;
using Serilog;

namespace Chunkyard
{
    internal class Command
    {
        public const int LatestLogPosition = -1;

        private static readonly string CacheDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "chunkyard",
            "cache");

        private static readonly ILogger _log = Log.ForContext<Command>();

        public static void PreviewFiles(PreviewOptions o)
        {
            foreach (var file in FileFetcher.Find(o.Files, o.ExcludePatterns))
            {
                Console.WriteLine(file);
            }
        }

        public static void CreateSnapshot(CreateOptions o)
        {
            _log.Information("Creating new snapshot");

            var snapshotBuilder = CreateSnapshotBuilder(o.Repository, o.Cached);

            foreach (var file in FileFetcher.Find(o.Files, o.ExcludePatterns))
            {
                using var fileStream = File.OpenRead(file);
                snapshotBuilder.AddContent(fileStream, file);
            }

            var newLogPosition = snapshotBuilder.WriteSnapshot(DateTime.Now);

            _log.Information(
                "Latest snapshot is at {LogPosition}",
                newLogPosition);
        }

        public static void VerifySnapshot(VerifyOptions o)
        {
            _log.Information("Verifying snapshot {LogPosition}", o.LogPosition);

            CreateSnapshotBuilder(o.Repository)
                .VerifySnapshot(o.LogPosition, o.IncludeFuzzy, o.Shallow);
        }

        public static void RestoreSnapshot(RestoreOptions o)
        {
            _log.Information(
                "Restoring snapshot {LogPosition} to {Directory}",
                o.LogPosition,
                o.Directory);

            var mode = o.Overwrite
                ? FileMode.OpenOrCreate
                : FileMode.CreateNew;

            CreateSnapshotBuilder(o.Repository).RestoreSnapshot(
                o.LogPosition,
                (contentName) =>
                {
                    var file = Path.Combine(
                        Path.GetFullPath(o.Directory),
                        contentName);

                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    return new FileStream(file, mode, FileAccess.Write);
                },
                o.IncludeFuzzy);
        }

        public static void ShowLogPositions(LogOptions o)
        {
            var snapshots = CreateSnapshotBuilder(o.Repository)
                .GetSnapshots();

            foreach (var snapshotTuple in snapshots)
            {
                var logPosition = snapshotTuple.Item1;
                var snapshot = snapshotTuple.Item2;

                Console.WriteLine($"{logPosition}: {snapshot.CreationTime}");
            }
        }

        private static SnapshotBuilder CreateSnapshotBuilder(
            string repository,
            bool cached = false)
        {
            _log.Information("Using repository {Repository}", repository);

            var nonceGenerator = new NonceGenerator();
            IContentStore contentStore = new ContentStore(
                new FileRepository(repository),
                nonceGenerator,
                new ContentStoreConfig(
                    HashAlgorithmName.SHA256,
                    2 * 1024 * 1024,
                    4 * 1024 * 1024,
                    8 * 1024 * 1024));

            contentStore = cached
                ? new CachedContentStore(contentStore, CacheDirectoryPath)
                : contentStore;

            return SnapshotBuilder.OpenRepository(
                new EnvironmentPrompt(
                    new ConsolePrompt()),
                nonceGenerator,
                contentStore);
        }
    }
}
