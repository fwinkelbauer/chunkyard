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
                    "master",
                    HashAlgorithmName.SHA256,
                    2 * 1024 * 1024,
                    4 * 1024 * 1024,
                    8 * 1024 * 1024,
                    true);

                File.WriteAllText(
                    ConfigFilePath,
                    JsonConvert.SerializeObject(config));
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
            var logName = GetLogName(o.LogName);

            _log.Information("Creating new snapshot for log {LogName}", logName);

            var snapshotBuilder = CreateSnapshotBuilder(_repository.AnyLog(logName)
                ? ExistingPassword()
                : NewPassword());

            foreach (var filePath in FindFiles())
            {
                snapshotBuilder.AddContent(() => File.OpenRead(filePath), filePath);
            }

            var newLogPosition = snapshotBuilder.WriteSnapshot(logName, DateTime.Now);

            _log.Information("Latest snapshot is now {Uri}", Id.LogNameToUri(logName, newLogPosition));
        }

        public void VerifySnapshot(VerifyOptions o)
        {
            var uri = GetUri(o.RefLogId);
            _log.Information("Verifying snapshot {Uri}", uri);
            CreateSnapshotBuilder(ExistingPassword()).VerifySnapshot(uri);
        }

        public void RestoreSnapshot(RestoreOptions o)
        {
            var uri = GetUri(o.RefLogId);
            _log.Information("Restoring snapshot {Uri} to {Directory}", uri, o.Directory);

            CreateSnapshotBuilder(ExistingPassword()).Restore(
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
            var uri = GetUri(o.RefLogId);
            _log.Information("Listing files in snapshot {Uri}", uri);

            var names = CreateSnapshotBuilder(ExistingPassword())
                .List(uri, o.IncludeRegex);

            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
        }

        public void ListLog(LogOptions o)
        {
            var logName = GetLogName(o.LogName);

            foreach (var logPosition in _repository.ListLog(logName))
            {
                Console.WriteLine(Id.LogNameToUri(logName, logPosition));
            }
        }

        public void PushSnapshot(PushOptions o)
        {
            var logName = GetLogName(o.LogName);

            _log.Information("Pushing log {LogName}", logName);
            var remoteRepository = new FileRepository(o.Remote);

            CreateSnapshotBuilder(ExistingPassword())
                .Push(logName, remoteRepository);
        }

        public void PullSnapshot(PullOptions o)
        {
            var logName = GetLogName(o.LogName);

            _log.Information("Pulling log {LogName}", logName);
            var remoteRepository = new FileRepository(o.Remote);

            CreateSnapshotBuilder(remoteRepository, ExistingPassword())
                .Push(logName, _repository);
        }

        private string GetLogName(string logName)
        {
            return string.IsNullOrEmpty(logName)
                ? _config.LogName
                : logName;
        }

        private Uri GetUri(string refLogId)
        {
            if (string.IsNullOrEmpty(refLogId))
            {
                return Id.LogNameToUri(_config.LogName);
            }
            else
            {
                return new Uri(refLogId);
            }
        }

        private SnapshotBuilder CreateSnapshotBuilder(IRepository repository, string password)
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

            return new SnapshotBuilder(
                contentStore,
                password);
        }

        private SnapshotBuilder CreateSnapshotBuilder(string password)
        {
            return CreateSnapshotBuilder(_repository, password);
        }

        private static IEnumerable<string> FindFiles()
        {
            var filters = File.Exists(FiltersFilePath)
                ? File.ReadAllLines(FiltersFilePath)
                : Array.Empty<string>();

            return FileFetcher.FindRelative(Program.RootDirectoryPath, filters);
        }

        private static string NewPassword()
        {
            var firstPassword = ReadPassword("Enter new password: ");
            var secondPassword = ReadPassword("Re-enter password: ");

            if (!firstPassword.Equals(secondPassword))
            {
                throw new ChunkyardException("Passwords do not match");
            }

            return firstPassword;
        }

        private static string ExistingPassword()
        {
            return ReadPassword("Password: ");
        }

        // https://stackoverflow.com/questions/23433980/c-sharp-console-hide-the-input-from-console-window-while-typing
        private static string ReadPassword(string prompt)
        {
            Console.Write(prompt);

            var input = new StringBuilder();

            while (true)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace
                    && input.Length > 0)
                {
                    input.Remove(input.Length - 1, 1);
                }
                else
                {
                    input.Append(key.KeyChar);
                }
            }

            Console.WriteLine();

            return input.ToString();
        }
    }
}
