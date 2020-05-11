using CommandLine;

namespace Chunkyard.Options
{
    [Verb("log", HelpText = "List all log entries")]
    public class LogOptions
    {
        public LogOptions(string repository)
        {
            Repository = repository;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository")]
        public string Repository { get; }
    }
}
