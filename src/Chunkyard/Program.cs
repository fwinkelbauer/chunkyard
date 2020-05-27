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
                .WithParsed<PreviewOptions>(o => Command.PreviewFiles(o))
                .WithParsed<RestoreOptions>(o => Command.RestoreSnapshot(o))
                .WithParsed<CreateOptions>(o => Command.CreateSnapshot(o))
                .WithParsed<CheckOptions>(o => Command.CheckSnapshot(o))
                .WithParsed<ShowOptions>(o => Command.ShowSnapshot(o))
                .WithParsed<RemoveOptions>(o => Command.RemoveSnapshot(o))
                .WithParsed<ListOptions>(o => Command.ListSnapshots(o))
                .WithParsed<GarbageCollectOptions>(o => Command.GarbageCollect(o))
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
