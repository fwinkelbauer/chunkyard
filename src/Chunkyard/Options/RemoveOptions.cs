using CommandLine;

namespace Chunkyard.Options
{
    [Verb("remove", HelpText = "Removes a snapshot")]
    public class RemoveOptions
    {
        public RemoveOptions(
            string repository,
            int logPosition)
        {
            Repository = repository;
            LogPosition = logPosition;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository")]
        public string Repository { get; }

        [Option('l', "log-position", Required = true, HelpText = "The log position")]
        public int LogPosition { get; }
    }
}
