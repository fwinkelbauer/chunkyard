using CommandLine;

namespace Chunkyard.Cli
{
    [Verb("keep", HelpText = "Keep only the given list of snapshots.")]
    public class KeepOptions
    {
        public KeepOptions(
            string repository,
            int lastCount)
        {
            Repository = repository;
            LastCount = lastCount;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option("last", Required = true, HelpText = "The count of the last snapshots to keep")]
        public int LastCount { get; }
    }
}
