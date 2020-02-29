using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Chunkyard.Options;
using Serilog;

namespace Chunkyard
{
    public class Command
    {
        public const string RepositoryDirectoryName = ".chunkyard";
        public const string FiltersFileName = ".chunkyardfilter";
        public const string ConfigFileName = ".chunkyardconfig";

        private static readonly ILogger _log = Log.ForContext<Command>();

        private static readonly string RootDirectoryPath = Path.GetFullPath(".");
        private static readonly string ChunkyardDirectoryPath = Path.Combine(RootDirectoryPath, RepositoryDirectoryName);
        private static readonly string FiltersFilePath = Path.Combine(RootDirectoryPath, FiltersFileName);
        private static readonly string ConfigFilePath = Path.Combine(RootDirectoryPath, ConfigFileName);
        private static readonly string ContentDirectoryPath = Path.Combine(ChunkyardDirectoryPath, "content");
        private static readonly string RefLogDirectoryPath = Path.Combine(ChunkyardDirectoryPath, "reflog");

        private readonly ChunkyardConfig _config;
        private readonly IContentRefLog<FastCdcContentRef<LzmaContentRef<AesGcmContentRef<ContentRef>>>> _refLog;

        public Command()
        {
            _config = DataConvert.DeserializeObject<ChunkyardConfig>(
                File.ReadAllText(ConfigFilePath));

            _refLog = new FileRefLog<FastCdcContentRef<LzmaContentRef<AesGcmContentRef<ContentRef>>>>(
                RefLogDirectoryPath);
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
                    Crypto.GenerateSalt(),
                    10000);

                File.WriteAllText(
                    ConfigFilePath,
                    DataConvert.SerializeObject(config));
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

        public void CreateSnapshot()
        {
            _log.Information("Creating new snapshot for log {LogName}", _config.LogName);

            var snapshotter = CreateSnapshotter(_refLog.Any(_config.LogName)
                ? PromptPassword()
                : NewPassword());

            var newLogPosition = snapshotter.Write(
                _config.LogName,
                DateTime.Now,
                FindFiles(),
                (path) => File.OpenRead(path));

            _log.Information("Latest snapshot is now {Uri}", LogNameToUri(newLogPosition));
        }

        public void VerifySnapshot(VerifyOptions o)
        {
            var uri = RefLogIdToUri(o.RefLogId);
            _log.Information("Verifying snapshot {Uri}", uri);
            CreateSnapshotter(PromptPassword()).Verify(uri);
        }

        public void RestoreSnapshot(RestoreOptions o)
        {
            var uri = RefLogIdToUri(o.RefLogId);
            _log.Information("Restoring snapshot {Uri} to {Directory}", uri, o.Directory);

            CreateSnapshotter(PromptPassword()).Restore(
                uri,
                (docRef) =>
                {
                    var file = Path.Combine(o.Directory, docRef.Name);
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    return File.OpenWrite(file);
                },
                o.IncludeRegex);
        }

        public void DirSnapshot(DirOptions o)
        {
            var uri = RefLogIdToUri(o.RefLogId);
            _log.Information("Listing files in snapshot {Uri}", uri);

            var names = CreateSnapshotter(PromptPassword())
                .List(uri, o.IncludeRegex);

            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
        }

        public void ListLog()
        {
            foreach (var logPosition in _refLog.List(_config.LogName))
            {
                Console.WriteLine(LogNameToUri(logPosition));
            }
        }

        private static IEnumerable<string> FindFiles()
        {
            var filters = File.Exists(FiltersFilePath)
                ? File.ReadAllLines(FiltersFilePath)
                : Array.Empty<string>();

            return FileFetcher.FindRelative(RootDirectoryPath, filters);
        }

        private Snapshotter<FastCdcContentRef<LzmaContentRef<AesGcmContentRef<ContentRef>>>> CreateSnapshotter(string password)
        {
            var store = new FastCdcContentStore<LzmaContentRef<AesGcmContentRef<ContentRef>>>(
                new LzmaContentStore<AesGcmContentRef<ContentRef>>(
                    new AesGcmContentStore<ContentRef>(
                        new ContentStore(new FileStoreProvider(ContentDirectoryPath)),
                        Crypto.PasswordToKey(password, _config.Salt.ToArray(), _config.Iterations))),
                _config.MinChunkSizeInByte,
                _config.AvgChunkSizeInByte,
                _config.MaxChunkSizeInByte,
                Path.Combine(ChunkyardDirectoryPath, "tmp"));

            return new Snapshotter<FastCdcContentRef<LzmaContentRef<AesGcmContentRef<ContentRef>>>>(
                store,
                _refLog,
                _config.HashAlgorithmName);
        }

        private Uri LogNameToUri(int logPosition)
        {
            return new Uri($"log://{_config.LogName}?id={logPosition}");
        }

        private Uri RefLogIdToUri(string refLogId)
        {
            if (string.IsNullOrEmpty(refLogId))
            {
                return new Uri($"log://{_config.LogName}");
            }
            else
            {
                return new Uri(refLogId);
            }
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

        private static string PromptPassword()
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
