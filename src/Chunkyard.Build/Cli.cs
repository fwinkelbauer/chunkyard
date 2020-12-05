﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Chunkyard.Build.Options;

namespace Chunkyard.Build
{
    internal static class Cli
    {
        private const string ArtifactsDirectory = "artifacts";
        private const string Solution = "src/Chunkyard.sln";
        private const string Changelog = "CHANGELOG.md";

        private static readonly string Version = FetchVersion();
        private static readonly List<string> Executed = new List<string>();

        public static void Clean(DotnetOptions o)
        {
            Once(nameof(Clean), () =>
            {
                Dotnet(
                    $"clean {Solution}",
                    $"-c {o.Configuration}");

                var dirInfo = new DirectoryInfo(ArtifactsDirectory);

                if (!dirInfo.Exists)
                {
                    return;
                }

                foreach (var file in dirInfo.GetFiles())
                {
                    file.Delete();
                }

                foreach (var subDirInfo in dirInfo.GetDirectories())
                {
                    subDirInfo.Delete(true);
                }
            });
        }

        public static void Build(DotnetOptions o)
        {
            Once(nameof(Build), () =>
            {
                Dotnet(
                    $"build {Solution}",
                    $"-c {o.Configuration}",
                    "-warnaserror");

                Dotnet(
                    $"test {Solution}",
                    "--no-build",
                    $"-c {o.Configuration}");
            });
        }

        public static void Publish(DotnetOptions o)
        {
            Lint();
            Clean(o);
            Build(o);

            Once(nameof(Publish), () =>
            {
                Dotnet(
                    "publish src/Chunkyard",
                    $"-c {o.Configuration}",
                    $"-r {o.Runtime}",
                    $"-o {ArtifactsDirectory}",
                    $"-p:Version={Version}",
                    "-p:PublishSingleFile=true",
                    "-p:PublishTrimmed=true");
            });
        }

        public static void Lint()
        {
            Once(nameof(Lint), () =>
            {
                Dotnet(
                    $"format {Solution}",
                    "--check");

                Dotnet(
                    $"outdated {Solution}",
                    "--fail-on-updates",
                    "--exclude xunit.runner.visualstudio");
            });
        }

        public static void Fmt()
        {
            Once(nameof(Fmt), () =>
            {
                Dotnet($"format {Solution}");
            });
        }

        public static void Upgrade()
        {
            Once(nameof(Upgrade), () =>
            {
                Dotnet(
                    $"outdated {Solution}",
                    "--upgrade",
                    "--exclude xunit.runner.visualstudio");
            });
        }

        public static void Release()
        {
            Once(nameof(Release), () =>
            {
                var message = $"Prepare Chunkyard release v{Version}";
                var tag = $"v{Version}";

                Git("add -A");
                Git($"commit -m \"{message}\"");
                Git($"tag -a \"{tag}\" -m \"{message}\"");
            });
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
            int[] validExitCodes)
        {
            var startInfo = new ProcessStartInfo(
                fileName,
                string.Join(' ', arguments))
            {
                RedirectStandardOutput = true
            };

            using var process = Process.Start(startInfo);

            if (process == null)
            {
                throw new ExecuteException(
                    $"Could not start process '{fileName}'");
            }

            string? line;

            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                Console.WriteLine(line);
            }

            process.WaitForExit();

            if (!validExitCodes.Contains(process.ExitCode))
            {
                throw new ExecuteException(
                    $"Exit code of {fileName} was {process.ExitCode}");
            }
        }

        private static string FetchVersion()
        {
            var match = Regex.Match(
                File.ReadAllText(Changelog),
                @"##\s+(\d+\.\d+\.\d+)");

            return match.Groups.Count > 1
                ? match.Groups[1].Value
                : "0.0.0";
        }

        private static void Once(string name, Action action)
        {
            if (Executed.Contains(name))
            {
                return;
            }

            action();

            Executed.Add(name);
        }
    }
}
