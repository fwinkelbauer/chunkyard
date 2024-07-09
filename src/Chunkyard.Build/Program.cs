namespace Chunkyard.Build;

public static class Program
{
    private const string Solution = "src/Chunkyard.sln";
    private const string Configuration = "Release";

    static Program()
    {
        Directory.SetCurrentDirectory(
            GitQuery("rev-parse --show-toplevel"));

        Environment.SetEnvironmentVariable(
            "DOTNET_CLI_TELEMETRY_OPTOUT",
            "1");
    }

    public static int Main(string[] args)
    {
        return new CommandHandler()
            .With<BuildCommand>(
                new SimpleCommandParser(
                    "build",
                    "Build the repository",
                    new BuildCommand()),
                _ => Build())
            .With<CheckCommand>(
                new SimpleCommandParser(
                    "check",
                    "Check for dependency updates",
                    new CheckCommand()),
                _ => Check())
            .With<CleanCommand>(
                new SimpleCommandParser(
                    "clean",
                    "Clean the repository",
                    new CleanCommand()),
                _ => Clean())
            .With<PublishCommand>(
                new SimpleCommandParser(
                    "publish",
                    "Publish the main project",
                    new PublishCommand()),
                _ => Publish())
            .With<ReleaseCommand>(
                new SimpleCommandParser(
                    "release",
                    "Create a release commit",
                    new ReleaseCommand()),
                _ => Release())
            .Handle(args);
    }

    private static void Clean()
    {
        Announce("Cleanup");

        if (GitQuery("status --porcelain").Contains("??"))
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
        Announce("Build");

        Dotnet($"format {Solution} --verify-no-changes");

        Dotnet(
            $"build {Solution}",
            $"-c {Configuration}",
            "-warnaserror",
            "--tl:auto");

        Dotnet(
            $"test {Solution}",
            $"-c {Configuration}",
            "--no-build",
            "--nologo",
            "--tl:auto");
    }

    private static void Publish()
    {
        Clean();
        Build();

        var directory = "artifacts";
        var (tag, _, commit) = GitDescribe();
        var version = tag.TrimStart('v');

        foreach (var runtime in new[] { "linux-x64", "win-x64" })
        {
            Announce($"Publish {runtime}");

            Dotnet(
                "publish src/Chunkyard/Chunkyard.csproj",
                $"-c {Configuration}",
                $"-r {runtime}",
                "--self-contained",
                $"-o {directory}",
                $"-p:Version={version}",
                $"-p:SourceRevisionId={commit}",
                "-p:PublishSingleFile=true",
                "-p:PublishTrimmed=true",
                "-p:DebugType=embedded",
                "-p:ContinuousIntegrationBuild=true",
                "--tl:auto");
        }
    }

    private static void Check()
    {
        Announce("Check");

        Dotnet($"restore {Solution} --tl:auto");
        Dotnet($"list {Path.GetFullPath(Solution)} package --outdated");
    }

    private static void Release()
    {
        Announce("Release");

        var currentTag = GitQuery("describe --abbrev=0");
        Console.WriteLine($"Current tag: {currentTag}");
        Console.Write("New tag: ");
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
            GitQuery("describe --long"),
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
        ProcessUtils.Run("dotnet", string.Join(' ', arguments));
    }

    private static void Git(params string[] arguments)
    {
        ProcessUtils.Run("git", string.Join(' ', arguments));
    }

    private static string GitQuery(params string[] arguments)
    {
        return ProcessUtils.RunQuery("git", string.Join(' ', arguments));
    }

    private static void Announce(string text)
    {
        Console.WriteLine();
        Console.WriteLine(text);
        Console.WriteLine("========================================");
    }
}
