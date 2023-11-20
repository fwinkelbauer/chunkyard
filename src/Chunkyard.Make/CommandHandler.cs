namespace Chunkyard.Make;

/// <summary>
/// Handles every available command of the Chunkyard.Make assembly.
/// </summary>
internal static class CommandHandler
{
    private const string Solution = "src/Chunkyard.sln";
    private const string Changelog = "CHANGELOG.md";
    private const string Configuration = "Release";

    static CommandHandler()
    {
        Directory.SetCurrentDirectory(
            GitQuery("rev-parse --show-toplevel"));

        Environment.SetEnvironmentVariable(
            "DOTNET_CLI_TELEMETRY_OPTOUT",
            "1");
    }

    public static void Clean()
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

    public static void Build()
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
            "--nologo",
            "--logger console;verbosity=detailed");
    }

    public static void Publish()
    {
        Clean();
        Build();

        Announce("Publish");

        var directory = "artifacts";
        var version = FetchVersion();
        var commitId = GitQuery("rev-parse --short HEAD");

        foreach (var runtime in new[] { "linux-x64", "win-x64" })
        {
            var runtimeDirectory = Path.Combine(directory, runtime);

            Dotnet(
                "publish src/Chunkyard/Chunkyard.csproj",
                $"-c {Configuration}",
                $"-r {runtime}",
                "--self-contained",
                $"-o {runtimeDirectory}",
                $"-p:Version={version}",
                $"-p:SourceRevisionId={commitId}",
                "-p:PublishSingleFile=true",
                "-p:PublishTrimmed=true",
                "-p:DebugType=none");
        }
    }

    public static void Format()
    {
        Announce("Format");

        Dotnet($"format {Solution}");
    }

    public static void Check()
    {
        Announce("Check");

        Dotnet($"restore {Solution}");
        Dotnet($"list {Solution} package --outdated");
    }

    public static void Release()
    {
        Announce("Release");

        var version = FetchVersion();
        var tag = $"v{version}";
        var message = $"Prepare Chunkyard release {tag}";
        var status = GitQuery("status --porcelain");

        if (!status.Equals($" M {Changelog}")
            && !status.Equals($"M  {Changelog}"))
        {
            throw new InvalidOperationException(
                $"A release commit should only contain changes to {Changelog}");
        }

        Git($"commit -am \"{message}\"");
        Git($"tag -a \"{tag}\" -m \"{message}\"");
    }

    private static string FetchVersion()
    {
        var match = Regex.Match(
            File.ReadAllText(Changelog),
            @"##\s+(\d+\.\d+\.\d+)",
            RegexOptions.None,
            TimeSpan.FromSeconds(1));

        return match.Groups.Count < 2
            ? "0.1.0"
            : match.Groups[1].Value;
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
