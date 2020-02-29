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
            return Parser.Default.ParseArguments<InitOptions, FilterOptions, DirOptions, RestoreOptions, CreateOptions, VerifyOptions, LogOptions>(args).MapResult(
                (InitOptions _) => Run(Command.Init),
                (FilterOptions _) => Run(Command.Filter),
                (DirOptions o) => Run(() => new Command().DirSnapshot(o)),
                (RestoreOptions o) => Run(() => new Command().RestoreSnapshot(o)),
                (CreateOptions _) => Run(() => new Command().CreateSnapshot()),
                (VerifyOptions o) => Run(() => new Command().VerifySnapshot(o)),
                (LogOptions _) => Run(() => new Command().ListLog()),
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
