namespace Chunkyard.Build;

internal static class Commands
{
    private const string Artifacts = "artifacts";
    private const string Solution = "src/Chunkyard.sln";
    private const string MainProject = "src/Chunkyard/Chunkyard.csproj";
    private const string Changelog = "CHANGELOG.md";
    private const string Configuration = "Release";

    static Commands()
    {
        Directory.SetCurrentDirectory(
            GitQuery("rev-parse --show-toplevel"));
    }

    public static void Clean()
    {
        Dotnet(
            $"clean {Solution}",
            $"-c {Configuration}");

        BuildUtils.CleanDirectory(Artifacts);
    }

    public static void Build()
    {
        Dotnet(
            $"build {Solution}",
            $"-c {Configuration}",
            "-warnaserror");
    }

    public static void Test()
    {
        Dotnet(
            $"test {Solution}",
            $"-c {Configuration}");
    }

    public static void Ci()
    {
        Dotnet(
            $"format {Solution}",
            "--verify-no-changes");

        Build();
        Test();
    }

    public static void Publish()
    {
        ThrowOnUncommittedChanges();

        Clean();
        Ci();

        var version = FetchVersion();
        var commitId = GitQuery("rev-parse --short HEAD");

        foreach (var runtime in new[] { "win-x64", "linux-x64" })
        {
            var directory = Path.Combine(Artifacts, version, runtime);

            Dotnet(
                $"publish {MainProject}",
                $"-c {Configuration}",
                $"-r {runtime}",
                "--self-contained",
                $"-o {directory}",
                $"-p:Version={version}",
                $"-p:SourceRevisionId={commitId}",
                "-p:PublishSingleFile=true",
                "-p:PublishTrimmed=true");
        }
    }

    public static void Fmt()
    {
        Dotnet($"format {Solution}");
    }

    public static void Outdated()
    {
        Dotnet($"restore {Solution}");

        Dotnet($"list {Solution} package --deprecated --include-transitive");
        Dotnet($"list {Solution} package --vulnerable --include-transitive");
        Dotnet($"list {Solution} package --outdated");
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

    private static void ThrowOnUncommittedChanges()
    {
        if (GitQuery("status --porcelain").Length > 0)
        {
            throw new BuildException(
                "Publishing uncommitted changes is not allowed");
        }
    }

    private static string FetchVersion()
    {
        return BuildUtils.FetchChangelogVersion(Changelog);
    }

    private static void Dotnet(params string[] arguments)
    {
        BuildUtils.Exec("dotnet", arguments, new[] { 0 });
    }

    private static void Git(params string[] arguments)
    {
        BuildUtils.Exec("git", arguments, new[] { 0 });
    }

    private static string GitQuery(params string[] arguments)
    {
        return BuildUtils.ExecQuery("git", arguments, new[] { 0 });
    }
}
