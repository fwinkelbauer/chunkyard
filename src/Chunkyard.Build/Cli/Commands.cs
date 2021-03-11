using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Chunkyard.Build.Cli
{
    internal static class Commands
    {
        private const string ArtifactsDirectory = "artifacts";
        private const string Solution = "src/Chunkyard.sln";
        private const string Changelog = "CHANGELOG.md";

        private static readonly string Version = FetchVersion();
        private static readonly List<string> Executed = new List<string>();

        public static void Clean(DotnetOptions o) => Once(() =>
        {
            Dotnet(
                $"clean {Solution}",
                $"-c {o.Configuration}");

            CleanDirectory(ArtifactsDirectory);
        });

        public static void Build(DotnetOptions o) => Once(() =>
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

        public static void Publish(DotnetOptions o) => Once(() =>
        {
            Clean(o);
            Build(o);

            Dotnet(
                "publish src/Chunkyard",
                $"-c {o.Configuration}",
                $"-r {o.Runtime}",
                $"-o {ArtifactsDirectory}",
                $"-p:Version={Version}",
                "-p:PublishSingleFile=true",
                "-p:PublishTrimmed=true");
        });

        public static void Fmt() => Once(() =>
        {
            Dotnet($"format {Solution}");
        });

        public static void Release() => Once(() =>
        {
            var message = $"Prepare Chunkyard release v{Version}";
            var tag = $"v{Version}";

            Git("add -A");
            Git($"commit -m \"{message}\"");
            Git($"tag -a \"{tag}\" -m \"{message}\"");
        });

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

        private static void Once(
            Action action,
            [CallerMemberName] string memberName = "")
        {
            if (Executed.Contains(memberName))
            {
                return;
            }

            action();

            Executed.Add(memberName);
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
