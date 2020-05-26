using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Chunkyard.Build.Options;

namespace Chunkyard.Build
{
    public static class Command
    {
        private const string ArtifactsDirectory = "artifacts";
        private const string Solution = "src/Chunkyard.sln";

        private static readonly string Version = Regex.Match(
            File.ReadAllText("CHANGELOG.md"),
            @"##\s+(\d+\.\d+\.\d+)")
            .Groups[1].Value;

        public static void Lint()
        {
            Dotnet(
                "format",
                $"--workspace {Solution}",
                "--dry-run",
                "--check");
        }

        public static void Clean()
        {
            if (Directory.Exists(ArtifactsDirectory))
            {
                Directory.Delete(ArtifactsDirectory, true);
            }
        }

        public static void Build(DotnetOptions o)
        {
            o.EnsureNotNull(nameof(o));

            Dotnet(
                $"build {Solution}",
                $"-c {o.Configuration}",
                $"-r {o.Runtime}",
                "-warnaserror");
        }

        public static void Test(DotnetOptions o)
        {
            o.EnsureNotNull(nameof(o));

            Dotnet(
                $"test {Solution}",
                $"-c {o.Configuration}");
        }

        public static void Default()
        {
            var o = new DotnetOptions(
                DotnetOptions.DefaultConfiguration,
                DotnetOptions.DefaultRuntime);

            Lint();
            Build(o);
            Test(o);
        }

        public static void Publish(DotnetOptions o)
        {
            o.EnsureNotNull(nameof(o));

            Clean();
            Lint();
            Build(o);
            Test(o);

            Dotnet(
                $"publish src/Chunkyard",
                $"-c {o.Configuration}",
                $"-r {o.Runtime}",
                $"-o {ArtifactsDirectory}",
                "/p:PublishSingleFile=true",
                "/p:PublishReadyToRun=true",
                $"/p:Version={Version}");
        }

        public static void Fmt()
        {
            Dotnet(
                "format",
                $"--workspace {Solution}");
        }

        public static void Commit()
        {
            Git("add -A");
            Git($"commit -m \"Prepare Chunkyard release v{Version}\"");
            Git($"tag -m \"v{Version}\"");
        }

        private static void Dotnet(params string[] arguments)
        {
            Exec("dotnet", arguments, new[] { 0 });
        }

        private static void Git(params string[] arguments)
        {
            Exec("git", arguments, new[] { 0 });
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
