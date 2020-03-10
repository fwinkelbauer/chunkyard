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
        public const string RootDirectoryName = ".";
        public const string RepositoryDirectoryName = ".chunkyard";
        public const string FiltersFileName = ".chunkyardfilter";
        public const string ConfigFileName = ".chunkyardconfig";
        public const string DefaultLogName = "master";
        public const string DefaultLogId = "log://master/";

        private static readonly string RootDirectoryPath = Path.GetFullPath(RootDirectoryName);
        private static readonly string ChunkyardDirectoryPath = Path.Combine(RootDirectoryPath, RepositoryDirectoryName);
        private static readonly string FiltersFilePath = Path.Combine(RootDirectoryPath, FiltersFileName);
        private static readonly string ConfigFilePath = Path.Combine(RootDirectoryPath, ConfigFileName);
        private static readonly string CacheDirectoryPath = Path.Combine(ChunkyardDirectoryPath, "cache");

        private static readonly ILogger _log = Log.ForContext<Command>();

        public static void Init()
        {
            if (File.Exists(ConfigFilePath))
            {
                _log.Information("{File} already exists", ConfigFileName);
            }
            else
            {
                _log.Information("Creating {File}", ConfigFileName);

                var config = new ChunkyardConfig(
                    HashAlgorithmName.SHA256,
                    2 * 1024 * 1024,
                    4 * 1024 * 1024,
                    8 * 1024 * 1024);

                File.WriteAllText(
                    ConfigFilePath,
                    JsonConvert.SerializeObject(
                        config,
                        Formatting.Indented) + "\n");
            }

            if (File.Exists(FiltersFilePath))
            {
                _log.Information("{File} already exists", FiltersFileName);
            }
            else
            {
                _log.Information("Creating {File}", FiltersFileName);
                File.WriteAllText(FiltersFilePath, "& .\n");
            }
        }

        public static void Filter()
        {
            foreach (var file in FindFiles())
            {
                Console.WriteLine(file);
            }
        }

        public static void CreateSnapshot(CreateOptions o)
        {
            _log.Information("Creating new snapshot for log {LogName}", o.LogName);

            var snapshotBuilder = CreateSnapshotBuilder(o.Repository, o.Cached);

            foreach (var filePath in FindFiles())
            {
                snapshotBuilder.AddContent(() => File.OpenRead(filePath), filePath);
            }

            var newLogPosition = snapshotBuilder.WriteSnapshot(
                o.LogName,
                DateTime.Now,
                JsonConvert.DeserializeObject<ChunkyardConfig>(
                    File.ReadAllText(ConfigFilePath)));

            _log.Information("Latest snapshot is now {Uri}", Id.LogNameToUri(o.LogName, newLogPosition));
        }

        public static void VerifySnapshot(VerifyOptions o)
        {
            var logUri = new Uri(o.LogId);
            _log.Information("Verifying snapshot {LogUri}", logUri);
            CreateSnapshotBuilder(o.Repository)
                .VerifySnapshot(logUri, o.IncludeRegex);
        }

        public static void RestoreSnapshot(RestoreOptions o)
        {
            var logUri = new Uri(o.LogId);
            _log.Information("Restoring snapshot {LogUri} to {Directory}", logUri, o.Directory);

            var mode = o.Overwrite
                ? FileMode.OpenOrCreate
                : FileMode.CreateNew;

            CreateSnapshotBuilder(o.Repository).Restore(
                logUri,
                (contentName) =>
                {
                    var file = Path.Combine(Path.GetFullPath(o.Directory), contentName);
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    return new FileStream(file, mode, FileAccess.Write);
                },
                o.IncludeRegex);
        }

        public static void DirSnapshot(DirOptions o)
        {
            var logUri = new Uri(o.LogId);
            _log.Information("Listing files in snapshot {LogUri}", logUri);

            var names = CreateSnapshotBuilder(o.Repository)
                .List(logUri, o.IncludeRegex);

            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
        }

        public static void ListLogPositions(LogOptions o)
        {
            var repository = CreateRepository(o.Repository);

            foreach (var logPosition in repository.ListLogPositions(o.LogName))
            {
                Console.WriteLine(Id.LogNameToUri(o.LogName, logPosition));
            }
        }

        public static void ListLogNames(LogsOptions o)
        {
            var repository = CreateRepository(o.Repository);

            foreach (var logName in repository.ListLogNames())
            {
                Console.WriteLine(logName);
            }
        }

        public static void PushSnapshot(PushOptions o)
        {
            var destinationRepository = CreateRepository(o.DestinationRepository);

            _log.Information("Pushing log {LogName} to {Repository}", o.LogName, destinationRepository.RepositoryUri);
            CreateSnapshotBuilder(o.SourceRepository)
                .Push(o.LogName, destinationRepository);
        }

        public static void PullSnapshot(PullOptions o)
        {
            var sourceRepository = CreateRepository(o.SourceRepository);

            _log.Information("Pulling log {LogName} from {Repository}", o.LogName, sourceRepository.RepositoryUri);
            CreateSnapshotBuilder(o.DestinationRepository)
                .Push(o.LogName, sourceRepository);
        }

        private static SnapshotBuilder CreateSnapshotBuilder(string repositoryName, bool cached = false)
        {
            IContentStore contentStore = new ContentStore(CreateRepository(repositoryName));

            contentStore = cached
                ? new CachedContentStore(contentStore, CacheDirectoryPath)
                : contentStore;

            return new SnapshotBuilder(
                contentStore,
                new ConsolePrompt());
        }

        private static IRepository CreateRepository(string repositoryName)
        {
            if (!repositoryName.Contains("://"))
            {
                repositoryName = Path.GetFullPath(repositoryName);
            }

            var repositoryUri = new Uri(repositoryName);

            if (repositoryUri.IsFile)
            {
                return new FileRepository(repositoryUri.LocalPath);
            }
            else
            {
                throw new ChunkyardException($"Unsupported URI: {repositoryUri}");
            }
        }

        private static IEnumerable<string> FindFiles()
        {
            var filters = File.Exists(FiltersFilePath)
                ? File.ReadAllLines(FiltersFilePath)
                : Array.Empty<string>();

            return FileFetcher.FindRelative(
                RootDirectoryPath,
                filters);
        }
    }
}
