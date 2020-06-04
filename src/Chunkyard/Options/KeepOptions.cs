using CommandLine;

namespace Chunkyard.Options
{
    [Verb("keep", HelpText = "Keep only the given list of snapshots.")]
    public class KeepOptions
    {
        public KeepOptions(
            string repository,
            int latestCount)
        {
            Repository = repository;
            LatestCount = latestCount;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository")]
        public string Repository { get; }

        [Option('l', "latest", Required = true, HelpText = "The count of latest log positions to keep")]
        public int LatestCount { get; }
    }
}
