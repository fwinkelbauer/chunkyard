namespace Chunkyard.Make;

/// <summary>
/// Describes every available command line verb of the Chunkyard.Make assembly.
/// </summary>
internal static class Commands
{
    private const string Solution = "src/Chunkyard.sln";
    private const string Changelog = "CHANGELOG.md";
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

        Git(
            "clean -dfx",
            $"-e *{nameof(Chunkyard.Make)}",
            "-e .vs/",
            "-e launchSettings.json",
            "-e *.user");
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
        Clean();
        Build();

        var directory = "artifacts";
        var version = FetchVersion();
        var commitId = GitQuery("rev-parse --short HEAD");

        foreach (var runtime in new[] { "linux-x64", "win-x64" })
        {
            Dotnet(
                "publish src/Chunkyard/Chunkyard.csproj",
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

        GenerateChecksumFile(directory);
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

        var status = GitQuery("status --porcelain");

        if (!status.Equals($" M {Changelog}")
            && !status.Equals($"M  {Changelog}"))
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

    private static void GenerateChecksumFile(string directory)
    {
        var files = Directory.GetFiles(
            directory,
            "*",
            SearchOption.AllDirectories);

        var hashLines = new StringBuilder();

        foreach (var file in files)
        {
            var bytes = File.ReadAllBytes(file);

            var hash = Convert.ToHexString(SHA256.HashData(bytes))
                .ToLowerInvariant();

            var relativeFile = Path.GetRelativePath(directory, file);

            // The sha256sum binary expects Linux-style line endings
            hashLines.Append($"{hash} *{relativeFile}");
            hashLines.Append('\n');
        }

        File.WriteAllText(
            Path.Combine(directory, "SHA256SUMS"),
            hashLines.ToString());
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
