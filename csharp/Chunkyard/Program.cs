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
            return Parser.Default.ParseArguments<PreviewOptions, RestoreOptions, CreateOptions, VerifyOptions, LogOptions>(args).MapResult(
                (PreviewOptions o) => Run(() => Command.PreviewFiles(o)),
                (RestoreOptions o) => Run(() => Command.RestoreSnapshot(o)),
                (CreateOptions o) => Run(() => Command.CreateSnapshot(o)),
                (VerifyOptions o) => Run(() => Command.VerifySnapshot(o)),
                (LogOptions o) => Run(() => Command.ShowLogPositions(o)),
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
                return 1;
            }
        }
    }
}
