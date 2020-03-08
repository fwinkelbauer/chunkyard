using System;
using System.IO;
using Chunkyard.Options;
using CommandLine;
using Serilog;

namespace Chunkyard
{
    public static class Program
    {
        public const string RepositoryDirectoryName = ".chunkyard";

        public static readonly string RootDirectoryPath = Path.GetFullPath(".");
        public static readonly string ChunkyardDirectoryPath = Path.Combine(RootDirectoryPath, RepositoryDirectoryName);

        private static readonly string LogDirectoryPath = Path.Combine(ChunkyardDirectoryPath, "log");

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(LogDirectoryPath, "chunkyard.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 2)
                .CreateLogger();

            Environment.ExitCode = ProcessArguments(args);

            Log.CloseAndFlush();
        }

        private static int ProcessArguments(string[] args)
        {
            return Parser.Default.ParseArguments<InitOptions, FilterOptions, DirOptions, RestoreOptions, CreateOptions, VerifyOptions, LogOptions, LogsOptions, PushOptions, PullOptions>(args).MapResult(
                (InitOptions _) => Run(Command.Init),
                (FilterOptions _) => Run(Command.Filter),
                (DirOptions o) => Run(() => Command.DirSnapshot(o)),
                (RestoreOptions o) => Run(() => Command.RestoreSnapshot(o)),
                (CreateOptions o) => Run(() => Command.CreateSnapshot(o)),
                (VerifyOptions o) => Run(() => Command.VerifySnapshot(o)),
                (LogOptions o) => Run(() => Command.ListLogPositions(o)),
                (LogsOptions o) => Run(() => Command.ListLogNames(o)),
                (PushOptions o) => Run(() => Command.PushSnapshot(o)),
                (PullOptions o) => Run(() => Command.PullSnapshot(o)),
                _ => 1);
        }

        private static int Run(Action action)
        {
            try
            {
                action();
                return 0;
            }
            catch (Exception e)
            {
                Log.Error("{Error}", e.Message);
                Log.Debug(e, "An error occurred");
                return 1;
            }
        }
    }
}
