using System.Collections.Generic;
using CommandLine;

namespace Chunkyard.Options
{
    [Verb("keep", HelpText = "Keep only the given list of snapshots.")]
    public class KeepOptions
    {
        public KeepOptions(
            string repository,
            IEnumerable<int> logPositions)
        {
            Repository = repository;
            LogPositions = new List<int>(logPositions);
        }

        [Option('r', "repository", Required = true, HelpText = "The repository")]
        public string Repository { get; }

        [Option('l', "log-positions", Required = true, HelpText = "The log positions to keep")]
        public IEnumerable<int> LogPositions { get; }
    }
}
