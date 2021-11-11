namespace Chunkyard.Cli;

[Verb("cat", HelpText = "Export or print the value of a set of content URIs.")]
public class CatOptions
{
    public CatOptions(
        string repository,
        IEnumerable<Uri> contentUris,
        string? export)
    {
        Repository = repository;
        ContentUris = contentUris;
        Export = export;
    }

    [Option('r', "repository", Required = true, HelpText = "The repository path")]
    public string Repository { get; }

    [Option('c', "content", Required = true, HelpText = "The content URIs")]
    public IEnumerable<Uri> ContentUris { get; }

    [Option('e', "export", Required = false, HelpText = "The export path", Default = "")]
    public string? Export { get; }
}
