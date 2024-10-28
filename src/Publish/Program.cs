namespace Publish;

public static class Program
{
    public static int Main()
    {
        Directory.SetCurrentDirectory(
            GitCapture("rev-parse --show-toplevel").First());

        Environment.SetEnvironmentVariable(
            "DOTNET_CLI_TELEMETRY_OPTOUT",
            "1");

        try
        {
            Publish();
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.ToString());
            return 1;
        }
    }

    private static void Publish()
    {
        Clean();
        Build();

        var csproj = "src/Chunkyard/Chunkyard.csproj";
        var directory = "artifacts";
        var version = GitDescribe();

        foreach (var runtime in new[] { "linux-x64", "win-x64" })
        {
            Announce($"Publish {version} ({runtime})");

            Dotnet(
                $"publish {csproj}",
                $"-r {runtime}",
                $"-o {directory}",
                $"-p:Version={version}",
                "--self-contained",
                "-p:PublishSingleFile=true",
                "-p:PublishTrimmed=true",
                "-p:DebugType=embedded",
                "-p:ContinuousIntegrationBuild=true",
                "--tl:auto");
        }

        Announce($"Publish {version} (dotnet tools)");

        Dotnet(
            $"pack {csproj}",
            $"-o {directory}",
            $"-p:Version={version}",
            "-p:ContinuousIntegrationBuild=true",
            "--tl:auto");
    }

    private static void Clean()
    {
        Announce("Cleanup");

        if (GitCapture("status --porcelain").Any(l => l.Contains("??")))
        {
            throw new InvalidOperationException(
                "Found untracked files. Aborting cleanup");
        }

        Git(
            "clean -dfx",
            $"-e *{typeof(Program).Namespace}",
            "-e .vs/",
            "-e launchSettings.json");
    }

    private static void Build()
    {
        var solution = "src/Chunkyard.sln";

        Announce("Build");
        Dotnet($"format {solution} --verify-no-changes");
        Dotnet($"build {solution} -warnaserror --tl:auto");

        Announce("Test");
        Dotnet($"test {solution} --no-build");
    }

    private static string GitDescribe()
    {
        if (GitCapture("tag -l").Length == 0)
        {
            return "0.0.0";
        }

        var match = Regex.Match(
            GitCapture("describe --long").First(),
            @"^v(?<version>.*)-(?<distance>\d+)-g(?<commit>[a-f\d]+)$",
            RegexOptions.None,
            TimeSpan.FromSeconds(1));

        return match.Groups["version"].Value;
    }

    private static void Dotnet(params string[] arguments)
    {
        ProcessUtils.Run("dotnet", arguments);
    }

    private static void Git(params string[] arguments)
    {
        ProcessUtils.Run("git", arguments);
    }

    private static string[] GitCapture(params string[] arguments)
    {
        return ProcessUtils.Capture("git", arguments);
    }

    private static void Announce(string text)
    {
        Console.WriteLine();
        Console.WriteLine(text);
        Console.WriteLine("========================================");
    }
}
