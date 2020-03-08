using CommandLine;

namespace Chunkyard.Options
{
    [Verb("logs", HelpText = "Lists all content reference log names")]
    public class LogsOptions
    {
        public LogsOptions(string repository)
        {
            Repository = repository;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository", Default = Command.DefaultRepository)]
        public string Repository { get; }
    }
}
