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
            return Parser.Default.ParseArguments<InitOptions, FilterOptions, RestoreOptions, CreateOptions, VerifyOptions, LogOptions>(args).MapResult(
                (InitOptions _) => Run(Command.Init),
                (FilterOptions _) => Run(Command.Filter),
                (RestoreOptions o) => Run(() => Command.RestoreSnapshot(o)),
                (CreateOptions o) => Run(() => Command.CreateSnapshot(o)),
                (VerifyOptions o) => Run(() => Command.VerifySnapshot(o)),
                (LogOptions o) => Run(() => Command.ListLogPositions(o)),
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
