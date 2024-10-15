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
        Release();

        var directory = "artifacts";
        var (version, _) = GitDescribe();

        foreach (var runtime in new[] { "linux-x64", "win-x64" })
        {
            Announce($"Publish {version} ({runtime})");

            Dotnet(
                "publish src/Chunkyard/Chunkyard.csproj",
                "-c Release",
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
            "pack src/Chunkyard/Chunkyard.csproj",
            "-c Release",
            $"-o {directory}",
            $"-p:Version={version}",
            "-p:ContinuousIntegrationBuild=true",
            "--tl:auto");
    }

    private static void Clean()
    {
        Announce("Cleanup");

        if (GitCapture("status --porcelain").Contains("??"))
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

    private static void Release()
    {
        var (currentVersion, distance) = GitDescribe();

        if (distance == 0)
        {
            return;
        }

        Announce("Release");

        Console.WriteLine($"Current version tag: {currentVersion}");
        Console.Write("Create new version tag. Leave empty to skip: ");

        var newVersion = Console.ReadLine()?.Trim();
        var newTag = $"v{newVersion}";

        if (string.IsNullOrEmpty(newVersion))
        {
            return;
        }

        Git($"tag -a \"{newTag}\" -m \"Prepare release {newTag}\"");
    }

    private static (string Version, int Distance) GitDescribe()
    {
        if (GitCapture("tag -l").Length == 0)
        {
            return ("0.0.0", -1);
        }

        var match = Regex.Match(
            GitCapture("describe --long").First(),
            @"^(?<tag>.*)-(?<distance>\d+)-g(?<commit>[a-f0-9]+)$",
            RegexOptions.None,
            TimeSpan.FromSeconds(1));

        var tag = match.Groups["tag"].Value;
        var distance = Convert.ToInt32(match.Groups["distance"].Value);

        return (tag.TrimStart('v'), distance);
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
