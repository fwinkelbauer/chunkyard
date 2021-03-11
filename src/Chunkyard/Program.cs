using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Chunkyard.Cli;
using Chunkyard.Core;
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
            catch (CryptographicException)
            {
                WriteError("Error: Could not decrypt data");
            }
            catch (Exception e)
            {
                WriteError(e.ToString());
            }
        }

        private static void WriteError(string message)
        {
            Environment.ExitCode = 1;

            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static void ProcessArguments(string[] args)
        {
            var optionTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null)
                .ToArray();

            Parser.Default.ParseArguments(args, optionTypes)
                .WithParsed<PreviewOptions>(o => Commands.PreviewFiles(o))
                .WithParsed<RestoreOptions>(o => Commands.RestoreSnapshot(o))
                .WithParsed<CreateOptions>(o => Commands.CreateSnapshot(o))
                .WithParsed<CheckOptions>(o => Commands.CheckSnapshot(o))
                .WithParsed<ShowOptions>(o => Commands.ShowSnapshot(o))
                .WithParsed<RemoveOptions>(o => Commands.RemoveSnapshot(o))
                .WithParsed<KeepOptions>(o => Commands.KeepSnapshots(o))
                .WithParsed<ListOptions>(o => Commands.ListSnapshots(o))
                .WithParsed<GarbageCollectOptions>(o => Commands.GarbageCollect(o))
                .WithParsed<CopyOptions>(o => Commands.CopySnapshots(o))
                .WithParsed<DotOptions>(o => Commands.Dot(o))
                .WithNotParsed(_ => Environment.ExitCode = 1);
        }
    }
}
