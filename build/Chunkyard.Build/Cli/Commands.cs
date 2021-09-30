﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Chunkyard.Build.Cli
{
    internal static class Commands
    {
        private const string Artifacts = "artifacts";
        private const string BuildSolution = "build/Chunkyard.Build.sln";
        private const string SourceSolution = "src/Chunkyard.sln";
        private const string MainProject = "src/Chunkyard/Chunkyard.csproj";
        private const string Changelog = "CHANGELOG.md";

        static Commands()
        {
            Directory.SetCurrentDirectory(
                GitOutput("rev-parse --show-toplevel"));
        }

        public static void Clean(DotnetOptions o)
        {
            Dotnet(
                $"clean {SourceSolution}",
                $"-c {o.Configuration}");

            CleanDirectory(Artifacts);
        }

        public static void Build(DotnetOptions o)
        {
            Dotnet("tool restore");

            Dotnet(
                $"format {BuildSolution}",
                "--check");

            Dotnet(
                $"format {SourceSolution}",
                "--check");

            Dotnet(
                $"build {SourceSolution}",
                $"-c {o.Configuration}",
                "-warnaserror");
        }

        public static void Test(DotnetOptions o, bool live = false)
        {
            Dotnet(
                live
                    ? $"watch test --project {SourceSolution}"
                    : $"test {SourceSolution}",
                $"-c {o.Configuration}");
        }

        public static void Integrate(DotnetOptions o)
        {
            Build(o);
            Test(o);
        }

        public static void Publish(DotnetOptions o)
        {
            ThrowOnUncommittedChanges();

            Clean(o);
            Integrate(o);

            var version = FetchVersion();
            var commitId = GitOutput("rev-parse --short HEAD");

            foreach (var runtime in new[] { "win-x64", "linux-x64" })
            {
                var directory = Path.Combine(
                    Artifacts,
                    version,
                    runtime);

                Dotnet(
                    $"publish {MainProject}",
                    $"-c {o.Configuration}",
                    $"-r {runtime}",
                    $"-o {directory}",
                    $"-p:Version={version}",
                    $"-p:SourceRevisionId={commitId}",
                    "-p:PublishSingleFile=true",
                    "-p:PublishTrimmed=true",
                    "-p:TrimMode=Link");
            }
        }

        public static void Fmt()
        {
            Dotnet("tool restore");

            Dotnet($"format {BuildSolution}");
            Dotnet($"format {SourceSolution}");
        }

        public static void Release()
        {
            var version = FetchVersion();
            var tag = $"v{version}";
            var message = $"Prepare Chunkyard release {tag}";

            Git("reset");
            Git($"add {Changelog}");
            Git($"commit -m \"{message}\"");
            Git($"tag -a \"{tag}\" -m \"{message}\"");
        }

        private static void ThrowOnUncommittedChanges()
        {
            if (GitOutput("status --porcelain").Length > 0)
            {
                throw new BuildException(
                    "Publishing uncommitted changes is not allowed");
            }
        }

        private static void Dotnet(params string[] arguments)
        {
            Exec("dotnet", arguments, new[] { 0 });
        }

        private static void Git(params string[] arguments)
        {
            Exec("git", arguments, new[] { 0 });
        }

        private static string GitOutput(params string[] arguments)
        {
            var builder = new StringBuilder();

            Exec(
                "git",
                arguments,
                new[] { 0 },
                line => builder.AppendLine(line));

            return builder.ToString().Trim();
        }

        private static void Exec(
            string fileName,
            string[] arguments,
            int[] validExitCodes,
            Action<string>? processOutput = null)
        {
            var startInfo = new ProcessStartInfo(
                fileName,
                string.Join(' ', arguments))
            {
                RedirectStandardOutput = processOutput != null
            };

            using var process = Process.Start(startInfo);

            if (process == null)
            {
                throw new BuildException(
                    $"Could not start process '{fileName}'");
            }

            string? line;

            if (processOutput != null)
            {
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    processOutput(line);
                }
            }

            process.WaitForExit();

            if (!validExitCodes.Contains(process.ExitCode))
            {
                throw new BuildException(
                    $"Exit code of {fileName} was {process.ExitCode}");
            }
        }

        private static string FetchVersion()
        {
            var match = Regex.Match(
                File.ReadAllText(Changelog),
                @"##\s+(\d+\.\d+\.\d+)");

            if (match.Groups.Count < 2)
            {
                throw new BuildException(
                    "Could not fetch version from changelog");
            }

            return match.Groups[1].Value;
        }

        private static void CleanDirectory(string directory)
        {
            var dirInfo = new DirectoryInfo(directory);

            if (!dirInfo.Exists)
            {
                return;
            }

            foreach (var fileInfo in dirInfo.GetFiles())
            {
                fileInfo.Delete();
            }

            foreach (var subDirInfo in dirInfo.GetDirectories())
            {
                subDirInfo.Delete(true);
            }
        }
    }
}
