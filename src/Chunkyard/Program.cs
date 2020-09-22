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
                Console.WriteLine(e);
                Environment.ExitCode = 1;
            }
        }

        private static void ProcessArguments(string[] args)
        {
            Parser.Default.ParseArguments(args, LoadOptions())
                .WithParsed<PreviewOptions>(o => CLI.PreviewFiles(o))
                .WithParsed<RestoreOptions>(o => CLI.RestoreSnapshot(o))
                .WithParsed<CreateOptions>(o => CLI.CreateSnapshot(o))
                .WithParsed<CheckOptions>(o => CLI.CheckSnapshot(o))
                .WithParsed<ShowOptions>(o => CLI.ShowSnapshot(o))
                .WithParsed<RemoveOptions>(o => CLI.RemoveSnapshot(o))
                .WithParsed<KeepOptions>(o => CLI.KeepSnapshots(o))
                .WithParsed<ListOptions>(o => CLI.ListSnapshots(o))
                .WithParsed<GarbageCollectOptions>(o => CLI.GarbageCollect(o))
                .WithParsed<PushOptions>(o => CLI.PushSnapshots(o))
                .WithParsed<PullOptions>(o => CLI.PullSnapshots(o))
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
