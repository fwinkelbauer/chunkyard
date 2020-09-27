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
               ProcessArguments(
                   args ?? throw new ArgumentNullException(nameof(args)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Environment.ExitCode = 1;
            }
        }

        private static void ProcessArguments(string[] args)
        {
            if (args.Length == 0)
            {
                Cli.Default();
                return;
            }

            Parser.Default.ParseArguments(args, LoadOptions())
                .WithParsed<BuildOptions>(o => Cli.Build(o))
                .WithParsed<CleanOptions>(_ => Cli.Clean())
                .WithParsed<CommitOptions>(_ => Cli.Commit())
                .WithParsed<FmtOptions>(_ => Cli.Fmt())
                .WithParsed<PublishOptions>(o => Cli.Publish(o))
                .WithParsed<TestOptions>(o => Cli.Test(o))
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
