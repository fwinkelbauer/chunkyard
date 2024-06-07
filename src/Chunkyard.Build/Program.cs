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
            $"-e *{typeof(CommandHandler).Namespace}",
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
            "-warnaserror");

        Dotnet(
            $"test {Solution}",
            $"-c {Configuration}",
            "--no-build",
            "--nologo");
    }

    private static void Publish()
    {
        Clean();
        Build();

        Announce("Publish");

        var directory = "artifacts";
        var (version, revision) = FetchGitVersion();

        foreach (var runtime in new[] { "linux-x64", "win-x64" })
        {
            Dotnet(
                "publish src/Chunkyard/Chunkyard.csproj",
                $"-c {Configuration}",
                $"-r {runtime}",
                "--self-contained",
                $"-o {directory}",
                $"-p:Version={version}",
                $"-p:SourceRevisionId={revision}",
                "-p:PublishSingleFile=true",
                "-p:PublishTrimmed=true",
                "-p:DebugType=none");
        }
    }

    private static void Check()
    {
        Announce("Check");

        var solution = Path.Combine(Directory.GetCurrentDirectory(), Solution);

        Dotnet($"restore {solution}");
        Dotnet($"list {solution} package --outdated");
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

    private static (string, string) FetchGitVersion()
    {
        var version = GitQuery("describe --long").TrimStart('v');
        var split = version.Split("-", 2);

        return (split[0], split[1]);
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
        Console.WriteLine(text);
        Console.WriteLine("========================================");
    }
}
