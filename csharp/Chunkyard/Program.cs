using System;
using Chunkyard.Options;
using CommandLine;
using Serilog;

namespace Chunkyard
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            Environment.ExitCode = ProcessArguments(args);

            Log.CloseAndFlush();
        }

        private static int ProcessArguments(string[] args)
        {
            return Parser.Default.ParseArguments<InitOptions, FilterOptions, CleanOptions, CatOptions, RestoreOptions, CreateOptions, VerifyOptions, LogOptions, PushOptions, PullOptions>(args).MapResult(
                (InitOptions _) => Run(Command.Init),
                (FilterOptions _) => Run(Command.Filter),
                (CleanOptions _) => Run(Command.Clean),
                (CatOptions o) => Run(() => Command.CatSnapshot(o)),
                (RestoreOptions o) => Run(() => Command.RestoreSnapshot(o)),
                (CreateOptions o) => Run(() => Command.CreateSnapshot(o)),
                (VerifyOptions o) => Run(() => Command.VerifySnapshot(o)),
                (LogOptions o) => Run(() => Command.ListLogPositions(o)),
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
