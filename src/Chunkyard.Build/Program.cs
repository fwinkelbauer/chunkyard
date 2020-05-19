using System;
using System.Collections.Generic;
using System.Diagnostics;
using CommandLine;

namespace Chunkyard.Build
{
    public static class Program
    {
        private const string Solution = "src/Chunkyard.sln";

        public static void Main(string[] args)
        {
            Environment.ExitCode = ProcessArguments(args);
        }

        private static int ProcessArguments(string[] args)
        {
            return Parser.Default.ParseArguments<Options>(args).MapResult(
                (Options o) => Run(() => RunTarget(o)),
                _ => 1);
        }

        private static void RunTarget(Options o)
        {
            if (o.Target.Equals("lint"))
            {
                Dotnet($"format --workspace {Solution} --dry-run --check");
            }
            else if (o.Target.Equals("build"))
            {
                Dotnet($"build {Solution} -c {o.Configuration} -r {o.Runtime}");
            }
            else if (o.Target.Equals("publish"))
            {
                Dotnet($"publish src/Chunkyard/Chunkyard.csproj -c {o.Configuration} -r {o.Runtime} /p:PublishSingleFile=true /p:PublishReadyToRun=true");
            }
            else
            {
                throw new Exception(
                    $"Unknown target '{o.Target}'");
            }
        }

        private static void Dotnet(string arguments)
        {
            Exec("dotnet", arguments, new List<int> { 0 });
        }

        private static void Exec(
            string app,
            string arguments,
            List<int> validExitCodes)
        {
            var startInfo = new ProcessStartInfo(app, arguments)
            {
                RedirectStandardOutput = true
            };

            using var process = Process.Start(startInfo);
            string? line = string.Empty;

            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                Console.WriteLine(line);
            }

            process.WaitForExit();

            if (!validExitCodes.Contains(process.ExitCode))
            {
                throw new Exception(
                    $"Exit code of {app} was {process.ExitCode}");
            }
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
                Console.WriteLine(e.Message);
                return 1;
            }
        }
    }
}
