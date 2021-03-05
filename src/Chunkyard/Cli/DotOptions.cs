using CommandLine;

namespace Chunkyard.Cli
{
    [Verb("dot", HelpText = "Create backups based on a configuration file.")]
    public class DotOptions
    {
        public DotOptions(string? file)
        {
            File = file;
        }

        [Option('f', "file", Required = false, HelpText = "The configuration file", Default = null)]
        public string? File { get; }
    }
}
