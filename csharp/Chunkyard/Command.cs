using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Chunkyard.Core;
using Chunkyard.Options;
using Newtonsoft.Json;
using Serilog;

namespace Chunkyard
{
    internal class Command
    {
        public const string FiltersFileName = ".chunkyardfilter";
        public const string ConfigFileName = ".chunkyardconfig";
        public const string DefaultLogName = "master";
        public const string DefaultRefLog = "log://master";

        private static readonly string FiltersFilePath = Path.Combine(Program.RootDirectoryPath, FiltersFileName);
        private static readonly string ConfigFilePath = Path.Combine(Program.RootDirectoryPath, ConfigFileName);
        private static readonly string CacheDirectoryPath = Path.Combine(Program.ChunkyardDirectoryPath, "cache");

        private static readonly ILogger _log = Log.ForContext<Command>();

        private readonly ChunkyardConfig _config;
        private readonly IRepository _repository;

        public Command()
        {
            _config = JsonConvert.DeserializeObject<ChunkyardConfig>(
                File.ReadAllText(ConfigFilePath));

            _repository = new FileRepository(Program.ChunkyardDirectoryPath);
        }

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
                    8 * 1024 * 1024,
                    true);

                File.WriteAllText(
                    ConfigFilePath,
                    JsonConvert.SerializeObject(
                        config,
                        Formatting.Indented));
            }

            if (File.Exists(FiltersFilePath))
            {
                _log.Information("{File} already exists", FiltersFileName);
            }
            else
            {
                _log.Information("Creating {File}", FiltersFileName);
                File.WriteAllText(FiltersFilePath, string.Empty);
            }
        }

        public static void Filter()
        {
            foreach (var file in FindFiles())
            {
                Console.WriteLine(file);
            }
        }

        public void CreateSnapshot(CreateOptions o)
        {
            _log.Information("Creating new snapshot for log {LogName}", o.LogName);

            var snapshotBuilder = CreateSnapshotBuilder();

            foreach (var filePath in FindFiles())
            {
                snapshotBuilder.AddContent(() => File.OpenRead(filePath), filePath);
            }

            var newLogPosition = snapshotBuilder.WriteSnapshot(o.LogName, DateTime.Now);

            _log.Information("Latest snapshot is now {Uri}", Id.LogNameToUri(o.LogName, newLogPosition));
        }

        public void VerifySnapshot(VerifyOptions o)
        {
            var uri = new Uri(o.RefLogId);
            _log.Information("Verifying snapshot {Uri}", uri);
            CreateSnapshotBuilder().VerifySnapshot(uri);
        }

        public void RestoreSnapshot(RestoreOptions o)
        {
            var uri = new Uri(o.RefLogId);
            _log.Information("Restoring snapshot {Uri} to {Directory}", uri, o.Directory);

            CreateSnapshotBuilder().Restore(
                uri,
                (contentName) =>
                {
                    var file = Path.Combine(o.Directory, contentName);
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    return new FileStream(file, FileMode.CreateNew);
                },
                o.IncludeRegex);
        }

        public void DirSnapshot(DirOptions o)
        {
            var uri = new Uri(o.RefLogId);
            _log.Information("Listing files in snapshot {Uri}", uri);

            var names = CreateSnapshotBuilder()
                .List(uri, o.IncludeRegex);

            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
        }

        public void ListLogPositions(LogOptions o)
        {
            foreach (var logPosition in _repository.ListLogPositions(o.LogName))
            {
                Console.WriteLine(Id.LogNameToUri(o.LogName, logPosition));
            }
        }

        public void ListLogNames()
        {
            foreach (var logName in _repository.ListLogNames())
            {
                Console.WriteLine(logName);
            }
        }

        public void PushSnapshot(PushOptions o)
        {
            _log.Information("Pushing log {LogName}", o.LogName);
            var remoteRepository = new FileRepository(o.Remote);

            CreateSnapshotBuilder()
                .Push(o.LogName, remoteRepository);
        }

        public void PullSnapshot(PullOptions o)
        {
            _log.Information("Pulling log {LogName}", o.LogName);
            var remoteRepository = new FileRepository(o.Remote);

            CreateSnapshotBuilder(remoteRepository)
                .Push(o.LogName, _repository);
        }

        private SnapshotBuilder CreateSnapshotBuilder(IRepository repository)
        {
            IContentStore contentStore = new ContentStore(
                repository,
                _config.HashAlgorithmName,
                _config.MinChunkSizeInByte,
                _config.AvgChunkSizeInByte,
                _config.MaxChunkSizeInByte);

            if (_config.UseCache)
            {
                contentStore = new CachedContentStore(
                    contentStore,
                    CacheDirectoryPath);
            }

            return new SnapshotBuilder(contentStore, new ConsolePrompt());
        }

        private SnapshotBuilder CreateSnapshotBuilder()
        {
            return CreateSnapshotBuilder(_repository);
        }

        private static IEnumerable<string> FindFiles()
        {
            var filters = File.Exists(FiltersFilePath)
                ? File.ReadAllLines(FiltersFilePath)
                : Array.Empty<string>();

            return FileFetcher.FindRelative(Program.RootDirectoryPath, filters);
        }
    }
}
