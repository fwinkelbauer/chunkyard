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
        public static readonly string LogDirectoryPath = Path.Combine(RootDirectoryPath, "log");

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(LogDirectoryPath, "chunkyard.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 2)
                .CreateLogger();

            Environment.ExitCode = ProcessArguments(args);

            Log.CloseAndFlush();
        }

        private static int ProcessArguments(string[] args)
        {
            return Parser.Default.ParseArguments<InitOptions, FilterOptions, DirOptions, RestoreOptions, CreateOptions, VerifyOptions, LogOptions, PushOptions, PullOptions>(args).MapResult(
                (InitOptions _) => Run(Command.Init),
                (FilterOptions _) => Run(Command.Filter),
                (DirOptions o) => Run(() => new Command().DirSnapshot(o)),
                (RestoreOptions o) => Run(() => new Command().RestoreSnapshot(o)),
                (CreateOptions o) => Run(() => new Command().CreateSnapshot(o)),
                (VerifyOptions o) => Run(() => new Command().VerifySnapshot(o)),
                (LogOptions o) => Run(() => new Command().ListLog(o)),
                (PushOptions o) => Run(() => new Command().PushSnapshot(o)),
                (PullOptions o) => Run(() => new Command().PullSnapshot(o)),
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
