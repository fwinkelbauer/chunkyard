using System.Collections.Generic;
using CommandLine;

namespace Chunkyard.Cli
{
    [Verb("check", HelpText = "Check if a snapshot is valid.")]
    public class CheckOptions
    {
        public CheckOptions(
            string repository,
            int logPosition,
            IEnumerable<string> includePatterns,
            bool shallow)
        {
            Repository = repository;
            LogPosition = logPosition;
            IncludePatterns = includePatterns;
            Shallow = shallow;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID", Default = Commands.LatestLogPosition)]
        public int LogPosition { get; }

        [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for files to include")]
        public IEnumerable<string> IncludePatterns { get; }

        [Option("shallow", Required = false, HelpText = "Check if chunks exist without further validation", Default = false)]
        public bool Shallow { get; }
    }
}
