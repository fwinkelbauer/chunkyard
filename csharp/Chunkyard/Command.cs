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
        public const string LocalRepository = "local";
        public const string RemoteRepository = "remote";
        public const string RepositoryDirectoryName = ".chunkyard";
        public const string FiltersFileName = ".chunkyardfilter";
        public const string ConfigFileName = ".chunkyardconfig";
        public const int DefaultLogPosition = -1;

        private const string SnapshotLogName = "snapshot";

        private static readonly string RootDirectoryPath = Path.GetFullPath(".");
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
                    new Dictionary<string, string>
                    {
                        { "local", Path.Combine(".", RepositoryDirectoryName) }
                    },
                    HashAlgorithmName.SHA256,
                    2 * 1024 * 1024,
                    4 * 1024 * 1024,
                    8 * 1024 * 1024);

                File.WriteAllText(
                    ConfigFilePath,
                    JsonConvert.SerializeObject(
                        config,
                        Formatting.Indented) + Environment.NewLine);
            }

            if (File.Exists(FiltersFilePath))
            {
                _log.Information("{File} already exists", FiltersFileName);
            }
            else
            {
                _log.Information("Creating {File}", FiltersFileName);
                File.WriteAllText(FiltersFilePath, "& ." + Environment.NewLine);
            }
        }

        public static void Filter()
        {
            foreach (var file in FindFiles())
            {
                Console.WriteLine(file);
            }
        }

        public static void Clean()
        {
            foreach (var file in FindFiles())
            {
                _log.Information("Deleting {File}", file);
                File.Delete(file);
            }
        }

        public static void CreateSnapshot(CreateOptions o)
        {
            _log.Information("Creating new snapshot");

            var snapshotBuilder = CreateSnapshotBuilder(o.Repository, o.Cached);

            foreach (var filePath in FindFiles())
            {
                snapshotBuilder.AddContent(() => File.OpenRead(filePath), filePath);
            }

            var newLogPosition = snapshotBuilder.WriteSnapshot(
                SnapshotLogName,
                DateTime.Now,
                LoadConfig());

            _log.Information("Latest snapshot is now {Uri}", Id.LogNameToUri(SnapshotLogName, newLogPosition));
        }

        public static void VerifySnapshot(VerifyOptions o)
        {
            var logUri = Id.LogNameToUri(SnapshotLogName, o.LogPosition);
            _log.Information("Verifying snapshot {LogUri}", logUri);
            CreateSnapshotBuilder(o.Repository)
                .VerifySnapshot(logUri, o.IncludeFuzzy, o.Shallow);
        }

        public static void RestoreSnapshot(RestoreOptions o)
        {
            var logUri = Id.LogNameToUri(SnapshotLogName, o.LogPosition);
            _log.Information("Restoring snapshot {LogUri} to {Directory}", logUri, o.Directory);

            var mode = o.Overwrite
                ? FileMode.OpenOrCreate
                : FileMode.CreateNew;

            CreateSnapshotBuilder(o.Repository).RestoreSnapshot(
                logUri,
                (contentName) =>
                {
                    var file = Path.Combine(Path.GetFullPath(o.Directory), contentName);
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    return new FileStream(file, mode, FileAccess.Write);
                },
                o.IncludeFuzzy);
        }

        public static void CatSnapshot(CatOptions o)
        {
            var fuzzy = new Fuzzy(o.IncludeFuzzy);
            var logUri = Id.LogNameToUri(SnapshotLogName, o.LogPosition);

            var snapshot = CreateSnapshotBuilder(o.Repository).GetSnapshot(logUri);

            Console.WriteLine($"Uri: {logUri}");
            Console.WriteLine($"Created: {snapshot.CreationTime.ToString("yyyy-MM-dd HH:mm:ss")}");
            Console.WriteLine($"Hostname: {snapshot.Hostname}");
            Console.WriteLine("Content:");

            foreach (var contentReferences in snapshot.ContentReferences)
            {
                if (fuzzy.IsMatch(contentReferences.Name))
                {
                    Console.WriteLine($"- {contentReferences.Name}");
                }
            }
        }

        public static void ListLogPositions(LogOptions o)
        {
            var repository = CreateRepository(o.Repository);

            foreach (var logPosition in repository.ListLogPositions(SnapshotLogName))
            {
                Console.WriteLine(Id.LogNameToUri(SnapshotLogName, logPosition));
            }
        }

        public static void PushSnapshot(PushOptions o)
        {
            var sourceRepository = CreateRepository(o.SourceRepository);
            var destinationRepository = CreateRepository(o.DestinationRepository);

            _log.Information("Pushing log to {Repository}", destinationRepository.RepositoryUri);
            CreateSnapshotBuilder(sourceRepository)
                .Push(SnapshotLogName, destinationRepository);
        }

        public static void PullSnapshot(PullOptions o)
        {
            var sourceRepository = CreateRepository(o.SourceRepository);
            var destinationRepository = CreateRepository(o.DestinationRepository);

            _log.Information("Pulling log from {Repository}", sourceRepository.RepositoryUri);
            CreateSnapshotBuilder(sourceRepository)
                .Push(SnapshotLogName, destinationRepository);
        }

        private static SnapshotBuilder CreateSnapshotBuilder(string repositoryName, bool cached = false)
        {
            return CreateSnapshotBuilder(
                CreateRepository(repositoryName),
                cached);
        }

        private static SnapshotBuilder CreateSnapshotBuilder(IRepository repository, bool cached = false)
        {
            IContentStore contentStore = new ContentStore(repository);

            contentStore = cached
                ? new CachedContentStore(contentStore, CacheDirectoryPath)
                : contentStore;

            return new SnapshotBuilder(
                contentStore,
                new ConsolePrompt());
        }

        private static IRepository CreateRepository(string repositoryName)
        {
            if (File.Exists(ConfigFilePath)
                && LoadConfig().Lookup.TryGetValue(repositoryName, out var resolvedName))
            {
                repositoryName = resolvedName;
            }

            if (!repositoryName.Contains("://"))
            {
                repositoryName = Path.GetFullPath(repositoryName);
            }

            var repositoryUri = new Uri(repositoryName);

            _log.Information("Using repository {Repository}", repositoryUri);

            if (repositoryUri.IsFile)
            {
                return new FileRepository(repositoryUri.LocalPath);
            }
            else
            {
                throw new ChunkyardException($"Unsupported URI: {repositoryUri}");
            }
        }

        private static ChunkyardConfig LoadConfig()
        {
            return JsonConvert.DeserializeObject<ChunkyardConfig>(
                File.ReadAllText(ConfigFilePath));
        }

        private static IEnumerable<string> FindFiles()
        {
            return FileFetcher.FindRelative(
                RootDirectoryPath,
                File.ReadAllLines(FiltersFilePath));
        }
    }
}
