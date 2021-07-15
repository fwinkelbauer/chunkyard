using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Chunkyard.Build.Cli
{
    internal static class Commands
    {
        private static readonly string Root = Git("rev-parse --show-toplevel");
        private static readonly string Artifacts = Path.Combine(Root, "artifacts");
        private static readonly string Source = Path.Combine(Root, "src");
        private static readonly string MainProject = Path.Combine(Source, "Chunkyard");
        private static readonly string Changelog = Path.Combine(Root, "CHANGELOG.md");

        public static void Clean(DotnetOptions o)
        {
            Dotnet(
                $"clean {Source}",
                $"-c {o.Configuration}");

            CleanDirectory(Artifacts);
        }

        public static void Build(DotnetOptions o)
        {
            Tool();

            Dotnet(
                $"format {Source}",
                "--check");

            Dotnet(
                $"build {Source}",
                $"-c {o.Configuration}",
                "-warnaserror");

            Dotnet(
                $"test {Source}",
                $"-c {o.Configuration}",
                "--no-build");
        }

        public static void Publish(DotnetOptions o)
        {
            var dirty = Git("status --porcelain").Length > 0;

            if (dirty)
            {
                throw new BuildException(
                    "Publishing uncommited changes is not allowed");
            }

            Clean(o);
            Build(o);

            var version = FetchVersion();
            var commitId = Git("rev-parse --short HEAD");

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
            Tool();

            Dotnet($"format {Source}");
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

        private static void Tool()
        {
            Dotnet("tool restore");
        }

        private static void Dotnet(params string[] arguments)
        {
            Exec(
                "dotnet",
                arguments,
                new[] { 0 },
                Console.WriteLine);
        }

        private static string Git(params string[] arguments)
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
            Action<string> processOutput)
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

            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                processOutput(line);
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
