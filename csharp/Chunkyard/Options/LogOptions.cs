using CommandLine;

namespace Chunkyard.Options
{
    [Verb("log", HelpText = "Lists all log entries")]
    public class LogOptions
    {
        public LogOptions(string repository)
        {
            Repository = repository;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository", Default = Command.DefaultRepository)]
        public string Repository { get; }
    }
}
