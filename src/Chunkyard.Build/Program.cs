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
            if (args != null && args.Length == 0)
            {
                Environment.ExitCode = Run(Command.Build);
                return;
            }

            var result = Parser.Default.ParseArguments(args, LoadOptions());

            Environment.ExitCode = result.MapResult(
                (BuildOptions o) => Run(() => Command.Build(o)),
                (CleanOptions _) => Run(Command.Clean),
                (CommitOptions _) => Run(Command.Commit),
                (FmtOptions _) => Run(Command.Fmt),
                (LintOptions _) => Run(Command.Lint),
                (PublishOptions o) => Run(() => Command.Publish(o)),
                (TestOptions o) => Run(() => Command.Test(o)),
                _ => 1);
        }

        private static Type[] LoadOptions()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null)
                .ToArray();
        }

        private static int Run(Action action)
        {
            try
            {
                action();
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 1;
            }
        }
    }
}
