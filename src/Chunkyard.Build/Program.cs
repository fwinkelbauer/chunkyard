using System;
using System.Linq;
using System.Reflection;
using Chunkyard.Build.Options;
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
                Console.WriteLine($"Error: {e.Message}");
                Environment.ExitCode = 1;
            }
        }

        private static void ProcessArguments(string[] args)
        {
            var optionTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null)
                .ToArray();

            Parser.Default.ParseArguments(args, optionTypes)
                .WithParsed<CleanOptions>(o => Cli.Clean(o))
                .WithParsed<BuildOptions>(o => Cli.Build(o))
                .WithParsed<PublishOptions>(o => Cli.Publish(o))
                .WithParsed<ReleaseOptions>(_ => Cli.Release())
                .WithParsed<LintOptions>(_ => Cli.Lint())
                .WithParsed<FmtOptions>(_ => Cli.Fmt())
                .WithNotParsed(_ => Environment.ExitCode = 1);
        }
    }
}
