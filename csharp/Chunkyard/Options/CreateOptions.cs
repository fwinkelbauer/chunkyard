using CommandLine;

namespace Chunkyard.Options
{
    [Verb("create", HelpText = "Creates a new snapshot for a given log")]
    public class CreateOptions
    {
        public CreateOptions(string repository, string logName, bool cached)
        {
            Repository = repository;
            LogName = logName;
            Cached = cached;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository URI", Default = Command.RepositoryDirectoryName)]
        public string Repository { get; }

        [Option('l', "log-name", Required = false, HelpText = "The log name", Default = Command.DefaultLogName)]
        public string LogName { get; }

        [Option('c', "cached", Required = false, HelpText = "Use a file cache to improve performance", Default = false)]
        public bool Cached { get; }
    }
}
