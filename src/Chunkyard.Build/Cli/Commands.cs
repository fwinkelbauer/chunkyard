using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Chunkyard.Build.Cli
{
    internal static class Commands
    {
        private const string ArtifactsDirectory = "artifacts";
        private const string Solution = "src/Chunkyard.sln";
        private const string Changelog = "CHANGELOG.md";

        private static readonly List<string> Executed = new List<string>();

        public static void Setup() => Once(() =>
        {
            Dotnet("tool update -g dotnet-format");
        });

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
                $"format {Solution}",
                "--check");

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

            var version = FetchVersion();
            var commitId = Git("rev-parse --short HEAD");

            Dotnet(
                "publish src/Chunkyard",
                $"-c {o.Configuration}",
                $"-r {o.Runtime}",
                $"-o {ArtifactsDirectory}",
                $"-p:Version={version}",
                $"-p:SourceRevisionId={commitId}",
                "-p:PublishSingleFile=true",
                "-p:PublishTrimmed=true",
                "-p:TrimMode=Link");
        });

        public static void Fmt() => Once(() =>
        {
            Dotnet($"format {Solution}");
        });

        public static void Release() => Once(() =>
        {
            var version = FetchVersion();
            var message = $"Prepare Chunkyard release v{version}";
            var tag = $"v{version}";

            Git("add -A");
            Git($"commit -m \"{message}\"");
            Git($"tag -a \"{tag}\" -m \"{message}\"");
        });

        private static string Dotnet(params string[] arguments)
        {
            return Exec("dotnet", arguments, new[] { 0 });
        }

        private static string Git(params string[] arguments)
        {
            return Exec("git", arguments, new[] { 0 });
        }

        private static string Exec(
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
                throw new BuildException(
                    $"Could not start process '{fileName}'");
            }

            string? line;
            var builder = new StringBuilder();

            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                Console.WriteLine(line);
                builder.AppendLine(line);
            }

            process.WaitForExit();

            if (!validExitCodes.Contains(process.ExitCode))
            {
                throw new BuildException(
                    $"Exit code of {fileName} was {process.ExitCode}");
            }

            return builder.ToString();
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
