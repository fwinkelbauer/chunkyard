using System;
using System.Linq;
using System.Reflection;
using Chunkyard.Options;
using CommandLine;

namespace Chunkyard
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments(args, LoadOptions());

            Environment.ExitCode = result.MapResult(
                (PreviewOptions o) => Run(() => Command.PreviewFiles(o)),
                (RestoreOptions o) => Run(() => Command.RestoreSnapshot(o)),
                (BackupOptions o) => Run(() => Command.CreateSnapshot(o)),
                (CheckOptions o) => Run(() => Command.CheckSnapshot(o)),
                (ShowOptions o) => Run(() => Command.ShowSnapshot(o)),
                (RemoveOptions o) => Run(() => Command.RemoveSnapshot(o)),
                (LogOptions o) => Run(() => Command.ShowSnapshots(o)),
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
                Console.WriteLine(e);
                return 1;
            }
        }
    }
}
