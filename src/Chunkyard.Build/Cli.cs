using System;
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

        private static readonly string Version = Regex.Match(
            File.ReadAllText("CHANGELOG.md"),
            @"##\s+(\d+\.\d+\.\d+)")
            .Groups[1].Value;

        public static void Clean(DotnetOptions o)
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
        }

        public static void Build(DotnetOptions o)
        {
            Dotnet(
                $"build {Solution}",
                $"-c {o.Configuration}",
                "-warnaserror");

            Dotnet(
                $"test {Solution}",
                "--no-build",
                $"-c {o.Configuration}");
        }

        public static void Publish(DotnetOptions o)
        {
            Clean(o);

            Dotnet(
                $"format {Solution}",
                "--check");

            Build(o);

            Dotnet(
                "publish src/Chunkyard",
                $"-c {o.Configuration}",
                $"-r {o.Runtime}",
                $"-o {ArtifactsDirectory}",
                $"-p:Version={Version}",
                "-p:PublishSingleFile=true",
                "-p:PublishReadyToRun=true");
        }

        public static void Fmt()
        {
            Dotnet($"format {Solution}");
        }

        public static void Release()
        {
            var message = $"Prepare Chunkyard release v{Version}";
            var tag = $"v{Version}";

            Git("add -A");
            Git($"commit -m \"{message}\"");
            Git($"tag -a \"{tag}\" -m \"{message}\"");
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

            if (process == null)
            {
                throw new InvalidOperationException(
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
    }
}
