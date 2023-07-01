namespace Chunkyard.Make;

public sealed class CommandHandler : ICommandHandler
{
    private const string Solution = "src/Chunkyard.sln";
    private const string Changelog = "CHANGELOG.md";
    private const string Configuration = "Release";

    public CommandHandler()
    {
        Directory.SetCurrentDirectory(
            GitQuery("rev-parse --show-toplevel"));

        Environment.SetEnvironmentVariable(
            "DOTNET_CLI_TELEMETRY_OPTOUT",
            "1");
    }

    public void Handle(BuildCommand c)
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

    public void Handle(CheckCommand c)
    {
        Announce("Check");

        Dotnet($"restore {Solution}");

        Dotnet($"list {Solution} package --deprecated");
        Dotnet($"list {Solution} package --vulnerable");
        Dotnet($"list {Solution} package --outdated");
    }

    public void Handle(CleanCommand c)
    {
        Announce("Cleanup");

        if (GitQuery("status --porcelain").Contains("??"))
        {
            throw new InvalidOperationException(
                "Found untracked files. Aborting cleanup");
        }

        Git(
            "clean -dfx",
            $"-e *{GetType().Namespace}",
            "-e .vs/",
            "-e launchSettings.json");
    }

    public void Handle(FormatCommand c)
    {
        Announce("Format");

        Dotnet($"format {Solution}");
    }

    public void Handle(HelpCommand c)
    {
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"  {GetType().Assembly.GetName().Name} <command> <flags>");
        Console.WriteLine();

        foreach (var usage in c.Usages)
        {
            Console.WriteLine($"  {usage.Topic}");
            Console.WriteLine($"    {usage.Info}");
            Console.WriteLine();
        }

        if (c.Errors.Any())
        {
            Console.WriteLine(c.Errors.Count == 1
                ? "Error:"
                : "Errors:");

            foreach (var error in c.Errors)
            {
                Console.WriteLine($"  {error}");
            }

            Console.WriteLine();
        }

        Environment.ExitCode = 1;
    }

    public void Handle(PublishCommand c)
    {
        Handle(new CleanCommand());
        Handle(new BuildCommand());

        Announce("Publish");

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

    public void Handle(ReleaseCommand c)
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

        Array.Sort(files);

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

    private static void Announce(string text)
    {
        Console.WriteLine(text);
        Console.WriteLine("========================================");
    }
}

public interface ICommandHandler
{
    void Handle(BuildCommand c);

    void Handle(CheckCommand c);

    void Handle(CleanCommand c);

    void Handle(FormatCommand c);

    void Handle(HelpCommand c);

    void Handle(PublishCommand c);

    void Handle(ReleaseCommand c);
}
