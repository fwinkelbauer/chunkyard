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
                CLI.Default();
                return;
            }

            Parser.Default.ParseArguments(args, LoadOptions())
                .WithParsed<BuildOptions>(o => CLI.Build(o))
                .WithParsed<CleanOptions>(_ => CLI.Clean())
                .WithParsed<CommitOptions>(_ => CLI.Commit())
                .WithParsed<FmtOptions>(_ => CLI.Fmt())
                .WithParsed<PublishOptions>(o => CLI.Publish(o))
                .WithParsed<TestOptions>(o => CLI.Test(o))
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
