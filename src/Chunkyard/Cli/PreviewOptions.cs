namespace Chunkyard.Cli;

[Verb("preview", HelpText = "Preview the output of a create command.")]
public class PreviewOptions
{
    public PreviewOptions(
        string repository,
        IEnumerable<string> files,
        IEnumerable<string> excludePatterns)
    {
        Repository = repository;
        Files = files;
        ExcludePatterns = excludePatterns;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path")]
    public string Repository { get; }

    [Option('f', "files", Required = true, HelpText = "The files and directories to include")]
    public IEnumerable<string> Files { get; }

    [Option('e', "exclude", Required = false, HelpText = "The fuzzy patterns for files to exclude")]
    public IEnumerable<string> ExcludePatterns { get; }
}
