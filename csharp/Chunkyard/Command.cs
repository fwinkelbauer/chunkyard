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

        private const string DefaultProjectFile = ".chunkyardproject";

        private static readonly string CacheDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "chunkyard",
            "cache");

        private static readonly ILogger _log = Log.ForContext<Command>();

        public static void Init()
        {
            if (File.Exists(DefaultProjectFile))
            {
                _log.Information("{File} already exists", DefaultProjectFile);
            }
            else
            {
                _log.Information("Creating {File}", DefaultProjectFile);
                File.WriteAllText(DefaultProjectFile, "& ." + Environment.NewLine);
            }
        }

        public static void ShowFiles()
        {
            foreach (var file in FindFiles())
            {
                Console.WriteLine(file);
            }
        }

        public static void CreateSnapshot(CreateOptions o)
        {
            _log.Information("Creating new snapshot");

            var snapshotBuilder = CreateSnapshotBuilder(o.Repository, o.Cached);

            foreach (var filePath in FindFiles())
            {
                using var fileStream = File.OpenRead(filePath);
                snapshotBuilder.AddContent(fileStream, filePath);
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
            string repositoryPath,
            bool cached = false)
        {
            _log.Information("Using repository {Repository}", repositoryPath);

            var nonceGenerator = new NonceGenerator();
            IContentStore contentStore = new ContentStore(
                new FileRepository(repositoryPath),
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
                new ConsolePrompt(),
                nonceGenerator,
                contentStore);
        }

        private static IEnumerable<string> FindFiles()
        {
            return FileFetcher.Find(
                File.ReadAllLines(DefaultProjectFile));
        }
    }
}
