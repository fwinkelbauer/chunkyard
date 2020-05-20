using System;
using System.Linq;
using System.Reflection;
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
            return Parser.Default.ParseArguments(args, LoadOptions()).MapResult(
                (PreviewOptions o) => Run(() => Command.PreviewFiles(o)),
                (RestoreOptions o) => Run(() => Command.RestoreSnapshot(o)),
                (CreateOptions o) => Run(() => Command.CreateSnapshot(o)),
                (CheckOptions o) => Run(() => Command.CheckSnapshot(o)),
                (ListOptions o) => Run(() => Command.ListSnapshot(o)),
                (LogOptions o) => Run(() => Command.ShowLogPositions(o)),
                (GarbageCollectOptions o) => Run(() => Command.GarbageCollect(o)),
                _ => 1);
        }

        private static Type[] LoadOptions()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null)
                .ToArray();
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
                Log.Error(e, "Unexpected error");
                return 1;
            }
        }
    }
}
