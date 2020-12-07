using System;
using System.Linq;
using System.Reflection;
using Chunkyard.Build.Cli;
using CommandLine;

namespace Chunkyard.Build
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
                WriteError($"Error: {e.Message}");
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
                .WithParsed<CleanOptions>(o => Commands.Clean(o))
                .WithParsed<BuildOptions>(o => Commands.Build(o))
                .WithParsed<PublishOptions>(o => Commands.Publish(o))
                .WithParsed<ReleaseOptions>(_ => Commands.Release())
                .WithParsed<FmtOptions>(_ => Commands.Fmt())
                .WithParsed<UpgradeOptions>(_ => Commands.Upgrade())
                .WithNotParsed(_ => Environment.ExitCode = 1);
        }
    }
}
