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
            try
            {
                ProcessArguments(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.ExitCode = 1;
            }
        }

        private static void ProcessArguments(string[] args)
        {
            Parser.Default.ParseArguments(args, LoadOptions())
                .WithParsed<PreviewOptions>(o => Cli.PreviewFiles(o))
                .WithParsed<RestoreOptions>(o => Cli.RestoreSnapshot(o))
                .WithParsed<CreateOptions>(o => Cli.CreateSnapshot(o))
                .WithParsed<CheckOptions>(o => Cli.CheckSnapshot(o))
                .WithParsed<ShowOptions>(o => Cli.ShowSnapshot(o))
                .WithParsed<RemoveOptions>(o => Cli.RemoveSnapshot(o))
                .WithParsed<KeepOptions>(o => Cli.KeepSnapshots(o))
                .WithParsed<ListOptions>(o => Cli.ListSnapshots(o))
                .WithParsed<GarbageCollectOptions>(o => Cli.GarbageCollect(o))
                .WithParsed<PushOptions>(o => Cli.PushSnapshots(o))
                .WithParsed<PullOptions>(o => Cli.PullSnapshots(o))
                .WithNotParsed(_ => Environment.ExitCode = 1);
        }

        private static Type[] LoadOptions()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null)
                .ToArray();
        }
    }
}
