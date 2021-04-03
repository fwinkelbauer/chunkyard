using System.Collections.Generic;
using CommandLine;

namespace Chunkyard.Cli
{
    [Verb("show", HelpText = "Show the content of a snapshot.")]
    public class ShowOptions
    {
        public ShowOptions(
            string repository,
            int logPosition,
            IEnumerable<string> includePatterns)
        {
            Repository = repository;
            LogPosition = logPosition;
            IncludePatterns = includePatterns;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID", Default = Commands.LatestLogPosition)]
        public int LogPosition { get; }

        [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for files to include")]
        public IEnumerable<string> IncludePatterns { get; }
    }
}
