namespace Chunkyard.Make;

/// <summary>
/// Describes every available command line verb of the Chunkyard.Make assembly.
/// </summary>
internal static class Commands
{
    private const string Artifacts = "artifacts";
    private const string Solution = "src/Chunkyard.sln";
    private const string MainProject = "src/Chunkyard/Chunkyard.csproj";
    private const string Changelog = "CHANGELOG.md";
    private const string CleanIgnore = ".cleanignore";
    private const string Configuration = "Release";

    static Commands()
    {
        Directory.SetCurrentDirectory(
            GitQuery("rev-parse --show-toplevel"));
    }

    public static void Clean()
    {
        if (GitQuery("status --porcelain").Contains("??"))
        {
            throw new InvalidOperationException(
                $"Found untracked files. Aborting cleanup");
        }

        var expressions = File.ReadLines(CleanIgnore)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#"))
            .Select(l => $"-e {l}");

        Git(
            "clean -dfx",
            string.Join(' ', expressions));
    }

    public static void Build()
    {
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
        Build();

        var version = FetchVersion();
        var commitId = GitQuery("rev-parse --short HEAD");

        foreach (var runtime in new[] { "linux-x64", "win-x64" })
        {
            var directory = Path.Combine(Artifacts, runtime);

            Dotnet(
                $"publish {MainProject}",
                $"-c {Configuration}",
                $"-r {runtime}",
                "--self-contained",
                $"-o {directory}",
                $"-p:Version={version}",
                $"-p:SourceRevisionId={commitId}",
                "-p:PublishSingleFile=true",
                "-p:PublishTrimmed=true",
                "-p:DebugType=none");
        }
    }

    public static void Fmt()
    {
        Dotnet($"format {Solution}");
    }

    public static void Outdated()
    {
        Dotnet($"restore {Solution}");

        Dotnet($"list {Solution} package --deprecated");
        Dotnet($"list {Solution} package --vulnerable");
        Dotnet($"list {Solution} package --outdated");
    }

    public static void Release()
    {
        var version = FetchVersion();
        var tag = $"v{version}";
        var message = $"Prepare Chunkyard release {tag}";

        if (!GitQuery("status --porcelain").Equals($" M {Changelog}"))
        {
            throw new InvalidOperationException(
                $"A release commit should only contain changes to {Changelog}");
        }

        Git($"add {Changelog}");
        Git($"commit -m \"{message}\"");
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
}
