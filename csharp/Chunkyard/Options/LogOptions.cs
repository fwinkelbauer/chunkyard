using CommandLine;

namespace Chunkyard.Options
{
    [Verb("log", HelpText = "Lists all entries in a content reference log")]
    public class LogOptions
    {
        public LogOptions(string repository, string logName)
        {
            Repository = repository;
            LogName = logName;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository", Default = Command.RepositoryDirectoryName)]
        public string Repository { get; }

        [Option('l', "log-name", Required = false, HelpText = "The log name", Default = Command.DefaultLogName)]
        public string LogName { get; }
    }
}
