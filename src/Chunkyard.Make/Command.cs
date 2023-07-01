namespace Chunkyard.Make;

public sealed class BuildCommandParser : ICommandParser
{
    public string Command => "build";

    public string Info => "Build the repository";

    public ICommand Parse(ArgConsumer consumer)
    {
        return consumer.IsConsumed()
            ? new BuildCommand()
            : new HelpCommand(consumer.HelpTexts, consumer.Errors);
    }
}

public sealed class CheckCommandParser : ICommandParser
{
    public string Command => "check";

    public string Info => "Check for dependency updates";

    public ICommand Parse(ArgConsumer consumer)
    {
        return consumer.IsConsumed()
            ? new CheckCommand()
            : new HelpCommand(consumer.HelpTexts, consumer.Errors);
    }
}

public sealed class CleanCommandParser : ICommandParser
{
    public string Command => "clean";

    public string Info => "Clean the repository";

    public ICommand Parse(ArgConsumer consumer)
    {
        return consumer.IsConsumed()
            ? new CleanCommand()
            : new HelpCommand(consumer.HelpTexts, consumer.Errors);
    }
}

public sealed class FormatCommandParser : ICommandParser
{
    public string Command => "format";

    public string Info => "Run the formatter";

    public ICommand Parse(ArgConsumer consumer)
    {
        return consumer.IsConsumed()
            ? new FormatCommand()
            : new HelpCommand(consumer.HelpTexts, consumer.Errors);
    }
}

public sealed class PublishCommandParser : ICommandParser
{
    public string Command => "publish";

    public string Info => "Publish the main project";

    public ICommand Parse(ArgConsumer consumer)
    {
        return consumer.IsConsumed()
            ? new PublishCommand()
            : new HelpCommand(consumer.HelpTexts, consumer.Errors);
    }
}

public sealed class ReleaseCommandParser : ICommandParser
{
    public string Command => "release";

    public string Info => "Create a release commit";

    public ICommand Parse(ArgConsumer consumer)
    {
        return consumer.IsConsumed()
            ? new ReleaseCommand()
            : new HelpCommand(consumer.HelpTexts, consumer.Errors);
    }
}

public sealed class BuildCommand : ICommand
{
    public void Run()
    {
        CommandUtils.Announce("Build");

        CommandUtils.Dotnet($"format {CommandUtils.Solution} --verify-no-changes");

        CommandUtils.Dotnet(
            $"build {CommandUtils.Solution}",
            $"-c {CommandUtils.Configuration}",
            "-warnaserror");

        CommandUtils.Dotnet(
            $"test {CommandUtils.Solution}",
            $"-c {CommandUtils.Configuration}",
            "--no-build",
            "--nologo",
            "--logger console;verbosity=detailed");
    }
}

public sealed class CheckCommand : ICommand
{
    public void Run()
    {
        CommandUtils.Announce("Check");

        CommandUtils.Dotnet($"restore {CommandUtils.Solution}");

        CommandUtils.Dotnet($"list {CommandUtils.Solution} package --deprecated");
        CommandUtils.Dotnet($"list {CommandUtils.Solution} package --vulnerable");
        CommandUtils.Dotnet($"list {CommandUtils.Solution} package --outdated");
    }
}

public sealed class CleanCommand : ICommand
{
    public void Run()
    {
        CommandUtils.Announce("Cleanup");

        if (CommandUtils.GitQuery("status --porcelain").Contains("??"))
        {
            throw new InvalidOperationException(
                "Found untracked files. Aborting cleanup");
        }

        CommandUtils.Git(
            "clean -dfx",
            $"-e *{GetType().Namespace}",
            "-e .vs/",
            "-e launchSettings.json");
    }
}

public sealed class FormatCommand : ICommand
{
    public void Run()
    {
        CommandUtils.Announce("Format");

        CommandUtils.Dotnet($"format {CommandUtils.Solution}");
    }
}

public sealed class PublishCommand : ICommand
{
    public void Run()
    {
        new CleanCommand().Run();
        new BuildCommand().Run();

        CommandUtils.Announce("Publish");

        var directory = "artifacts";
        var version = CommandUtils.FetchVersion();
        var commitId = CommandUtils.GitQuery("rev-parse --short HEAD");

        foreach (var runtime in new[] { "linux-x64", "win-x64" })
        {
            CommandUtils.Dotnet(
                "publish src/Chunkyard/Chunkyard.csproj",
                $"-c {CommandUtils.Configuration}",
                $"-r {runtime}",
                "--self-contained",
                $"-o {directory}",
                $"-p:Version={version}",
                $"-p:SourceRevisionId={commitId}",
                "-p:PublishSingleFile=true",
                "-p:PublishTrimmed=true",
                "-p:DebugType=none");
        }

        CommandUtils.GenerateChecksumFile(directory);
    }
}

public sealed class ReleaseCommand : ICommand
{
    public void Run()
    {
        CommandUtils.Announce("Release");

        var version = CommandUtils.FetchVersion();
        var tag = $"v{version}";
        var message = $"Prepare Chunkyard release {tag}";
        var status = CommandUtils.GitQuery("status --porcelain");

        if (!status.Equals($" M {CommandUtils.Changelog}")
            && !status.Equals($"M  {CommandUtils.Changelog}"))
        {
            throw new InvalidOperationException(
                $"A release commit should only contain changes to {CommandUtils.Changelog}");
        }

        CommandUtils.Git($"add {CommandUtils.Changelog}");
        CommandUtils.Git($"commit -m \"{message}\"");
        CommandUtils.Git($"tag -a \"{tag}\" -m \"{message}\"");
    }
}

public static class CommandUtils
{
    public const string Solution = "src/Chunkyard.sln";
    public const string Changelog = "CHANGELOG.md";
    public const string Configuration = "Release";

    public static string FetchVersion()
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

    public static void GenerateChecksumFile(string directory)
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

    public static void Dotnet(params string[] arguments)
    {
        ProcessUtils.Run("dotnet", string.Join(' ', arguments));
    }

    public static void Git(params string[] arguments)
    {
        ProcessUtils.Run("git", string.Join(' ', arguments));
    }

    public static string GitQuery(params string[] arguments)
    {
        return ProcessUtils.RunQuery("git", string.Join(' ', arguments));
    }

    public static void Announce(string text)
    {
        Console.WriteLine(text);
        Console.WriteLine("========================================");
    }

}
