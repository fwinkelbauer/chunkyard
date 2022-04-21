namespace Chunkyard.Build;

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
                "-p:PublishTrimmed=true",
                "-p:PublishReadyToRun=true");
        }
    }

    public static void Install()
    {
        Publish();

        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var runtime = isWindows ? "win-x64" : "linux-x64";
        var name = isWindows ? "chunkyard.exe" : "chunkyard";
        var targetDirectory = isWindows
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "bin")
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local/bin");

        var source = Path.Combine(Artifacts, runtime, name);
        var target = Path.Combine(targetDirectory, name);

        Console.WriteLine($"Copying {source} to {target}");

        Directory.CreateDirectory(targetDirectory);
        File.Copy(source, target, true);
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
            throw new BuildException(
                $"A release commit should only contain changes to {Changelog}");
        }

        Git($"add {Changelog}");
        Git($"commit -m \"{message}\"");
        Git($"tag -a \"{tag}\" -m \"{message}\"");
    }

    private static string FetchVersion()
    {
        return BuildUtils.FetchChangelogVersion(Changelog);
    }

    private static void Dotnet(params string[] arguments)
    {
        BuildUtils.Run("dotnet", arguments, new[] { 0 });
    }

    private static void Git(params string[] arguments)
    {
        BuildUtils.Run("git", arguments, new[] { 0 });
    }

    private static string GitQuery(params string[] arguments)
    {
        return BuildUtils.RunQuery("git", arguments, new[] { 0 });
    }
}
