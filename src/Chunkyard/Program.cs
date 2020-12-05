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
            catch (ChunkyardException e)
            {
                WriteError($"Error: {e.Message}");
            }
            catch (Exception e)
            {
                WriteError(e.ToString());
            }
        }

        private static void WriteError(string message)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
            }
            finally
            {
                Console.ResetColor();
            }

            Environment.ExitCode = 1;
        }

        private static void ProcessArguments(string[] args)
        {
            var optionTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null)
                .ToArray();

            Parser.Default.ParseArguments(args, optionTypes)
                .WithParsed<PreviewOptions>(o => Cli.PreviewFiles(o))
                .WithParsed<RestoreOptions>(o => Cli.RestoreSnapshot(o))
                .WithParsed<CreateOptions>(o => Cli.CreateSnapshot(o))
                .WithParsed<CheckOptions>(o => Cli.CheckSnapshot(o))
                .WithParsed<ShowOptions>(o => Cli.ShowSnapshot(o))
                .WithParsed<RemoveOptions>(o => Cli.RemoveSnapshot(o))
                .WithParsed<KeepOptions>(o => Cli.KeepSnapshots(o))
                .WithParsed<ListOptions>(o => Cli.ListSnapshots(o))
                .WithParsed<GarbageCollectOptions>(o => Cli.GarbageCollect(o))
                .WithParsed<CopyOptions>(o => Cli.CopySnapshots(o))
                .WithNotParsed(_ => Environment.ExitCode = 1);
        }
    }
}
