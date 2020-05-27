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
            args.EnsureNotNull(nameof(args));

            try
            {
                if (args.Length == 0)
                {
                    Command.Default();
                    return;
                }

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
                .WithParsed<BuildOptions>(o => Command.Build(o))
                .WithParsed<CleanOptions>(_ => Command.Clean())
                .WithParsed<CommitOptions>(_ => Command.Commit())
                .WithParsed<FmtOptions>(_ => Command.Fmt())
                .WithParsed<LintOptions>(_ => Command.Lint())
                .WithParsed<PublishOptions>(o => Command.Publish(o))
                .WithParsed<TestOptions>(o => Command.Test(o))
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
