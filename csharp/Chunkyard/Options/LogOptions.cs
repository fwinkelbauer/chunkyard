using CommandLine;

namespace Chunkyard.Options
{
    [Verb("log", HelpText = "Lists all entries in the content reference log")]
    public class LogOptions
    {
        public LogOptions(string repository)
        {
            Repository = repository;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository", Default = Command.RepositoryDirectoryName)]
        public string Repository { get; }
    }
}
