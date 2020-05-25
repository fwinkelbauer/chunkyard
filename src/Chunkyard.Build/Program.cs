using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Chunkyard.Build
{
    public static class Program
    {
        private const string ArtifactsDirectory = "artifacts";
        private const string Solution = "src/Chunkyard.sln";
        private const string Configuration = "Release";

        private static readonly string Version = Regex.Match(
            File.ReadAllText("CHANGELOG.md"),
            @"##\s+(\d+\.\d+\.\d+)")
            .Groups[1].Value;

        private static readonly string Runtime =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "win-x64"
            : "linux-x64";

        private static readonly Dictionary<string, Action> RunTargets =
            new Dictionary<string, Action>(
                StringComparer.OrdinalIgnoreCase)
            {
                { "clean", Clean },
                { "lint", Lint },
                { "build", Build },
                { "test", Test },
                { "publish", Publish },
                { "preparerelease", PrepareRelease },
                { "help", Help }
            };

        public static void Main(string[] args)
        {
            var target = args?.Length == 1
                ? args[0]
                : "build";

            if (args == null
                || (args.Length != 0 && args.Length != 1)
                || !RunTargets.ContainsKey(target))
            {
                Help();

                Environment.ExitCode = 1;
                return;
            }

            try
            {
                RunTargets[target]();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.ExitCode = 1;
            }
        }

        private static void Clean()
        {
            if (Directory.Exists(ArtifactsDirectory))
            {
                Directory.Delete(ArtifactsDirectory, true);
            }
        }

        private static void Lint()
        {
            Dotnet(
                "format",
                $"--workspace {Solution}",
                "--dry-run",
                "--check");
        }

        private static void Build()
        {
            Dotnet(
                $"build {Solution}",
                $"-c {Configuration}",
                $"-r {Runtime}",
                "-warnaserror");
        }

        private static void Test()
        {
            Dotnet(
                $"test {Solution}",
                $"-c {Configuration}");
        }

        private static void Publish()
        {
            Clean();
            Lint();
            Test();

            Dotnet(
                $"publish src/Chunkyard",
                $"-c {Configuration}",
                $"-r {Runtime}",
                $"-o {ArtifactsDirectory}",
                "/p:PublishSingleFile=true",
                "/p:PublishReadyToRun=true",
                $"/p:Version={Version}");
        }

        private static void PrepareRelease()
        {
            Git("add -A");
            Git($"commit -m \"Prepare Chunkyard release v{Version}\"");
            Git($"tag -m \"v{Version}\"");
        }

        private static void Help()
        {
            Console.WriteLine("Usage: <target>");
            Console.WriteLine("Available targets:");

            foreach (var key in RunTargets.Keys)
            {
                Console.WriteLine($"- {key}");
            }
        }

        private static void Git(params string[] arguments)
        {
            Exec("git", arguments, new[] { 0 });
        }

        private static void Dotnet(params string[] arguments)
        {
            Exec("dotnet", arguments, new[] { 0 });
        }

        private static void Exec(
            string fileName,
            string[] arguments,
            IEnumerable<int> validExitCodes)
        {
            var startInfo = new ProcessStartInfo(
                fileName,
                string.Join(' ', arguments))
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
                    $"Exit code of {fileName} was {process.ExitCode}");
            }
        }
    }
}
