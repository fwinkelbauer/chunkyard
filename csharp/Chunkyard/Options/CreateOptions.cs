using CommandLine;

namespace Chunkyard.Options
{
    [Verb("create", HelpText = "Creates a new snapshot for a given log")]
    public class CreateOptions
    {
        public CreateOptions(string repository, string logName)
        {
            Repository = repository;
            LogName = logName;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository URI", Default = Command.DefaultRepository)]
        public string Repository { get; }

        [Option('l', "log-name", Required = false, HelpText = "The log name", Default = Command.DefaultLogName)]
        public string LogName { get; }
    }
}
