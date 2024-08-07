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
        Build();
        Release();

        var directory = "artifacts";

        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }

        var (tag, _, commit) = GitDescribe();
        var version = tag.TrimStart('v');

        foreach (var runtime in new[] { "linux-x64", "win-x64" })
        {
            Announce($"Publish {tag} ({runtime})");

            Dotnet(
                "publish src/Chunkyard/Chunkyard.csproj",
                "-c Release",
                $"-r {runtime}",
                $"-o {directory}",
                $"-p:Version={version}",
                $"-p:SourceRevisionId={commit}",
                "--self-contained",
                "-p:PublishSingleFile=true",
                "-p:PublishTrimmed=true",
                "-p:DebugType=embedded",
                "-p:ContinuousIntegrationBuild=true",
                "--tl:auto");
        }
    }

    private static void Build()
    {
        Announce("Build");

        var solution = "src/Chunkyard.sln";

        Dotnet($"format {solution} --verify-no-changes");
        Dotnet($"build {solution} -warnaserror --tl:auto");

        Announce("Test");

        Dotnet($"test {solution} --no-build");
    }

    private static void Release()
    {
        var (currentTag, distance, _) = GitDescribe();

        if (distance == 0)
        {
            return;
        }

        Announce("Release");

        Console.WriteLine($"Current tag: {currentTag}");
        Console.Write("Enter new tag. Leave empty to skip: ");

        var newTag = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(newTag))
        {
            return;
        }

        Git($"tag -a \"{newTag}\" -m \"Prepare Chunkyard release {newTag}\"");
    }

    private static (string Tag, int Distance, string Commit) GitDescribe()
    {
        var match = Regex.Match(
            GitCapture("describe --long").First(),
            @"^(?<tag>.*)-(?<distance>\d+)-g(?<hash>[a-f0-9]+)$",
            RegexOptions.None,
            TimeSpan.FromSeconds(1));

        var tag = match.Groups["tag"].Value;
        var distance = Convert.ToInt32(match.Groups["distance"].Value);
        var hash = match.Groups["hash"].Value;

        return (tag, distance, hash);
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
