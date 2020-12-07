using CommandLine;

namespace Chunkyard.Cli
{
    [Verb("remove", HelpText = "Remove a snapshot.")]
    public class RemoveOptions
    {
        public RemoveOptions(
            string repository,
            int logPosition)
        {
            Repository = repository;
            LogPosition = logPosition;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option('s', "snapshot", Required = true, HelpText = "The snapshot ID")]
        public int LogPosition { get; }
    }
}
