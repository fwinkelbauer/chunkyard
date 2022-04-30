namespace Chunkyard.Build;

/// <summary>
/// Describes every available command line verb of the Chunkyard.Build assembly.
/// </summary>
internal static class Commands
{
    private const string Artifacts = "artifacts";
    private const string Solution = "src/Chunkyard.sln";
    private const string MainProject = "src/Chunkyard/Chunkyard.csproj";
    private const string Changelog = "CHANGELOG.md";
    private const string Configuration = "Release";

    public static void Clean()
    {
        Git(
            "clean -dfx",
            "-e .vs",
            "-e launchSettings.json",
            "-e *.Build");
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
        Build();
        Test();
    }

    public static void Publish()
    {
        Clean();
        Ci();

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
            @"##\s+(\d+\.\d+\.\d+)");

        if (match.Groups.Count < 2)
        {
            throw new InvalidOperationException(
                $"Could not fetch version from {Changelog}");
        }

        return match.Groups[1].Value;
    }

    private static void Dotnet(params string[] arguments)
    {
        ProcessUtils.Run(
            new ProcessStartInfo("dotnet", string.Join(' ', arguments)));
    }

    private static void Git(params string[] arguments)
    {
        ProcessUtils.Run(
            new ProcessStartInfo("git", string.Join(' ', arguments)));
    }

    private static string GitQuery(params string[] arguments)
    {
        return ProcessUtils.RunQuery(
            new ProcessStartInfo("git", string.Join(' ', arguments)));
    }
}
