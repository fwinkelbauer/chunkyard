using CommandLine;

namespace Chunkyard.Options
{
    [Verb("check", HelpText = "Check if a snapshot is valid.")]
    public class CheckOptions
    {
        public CheckOptions(
            string repository,
            int logPosition,
            string includeFuzzy,
            bool shallow)
        {
            Repository = repository;
            LogPosition = logPosition;
            IncludeFuzzy = includeFuzzy;
            Shallow = shallow;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID", Default = Cli.LatestLogPosition)]
        public int LogPosition { get; }

        [Option('i', "include", Required = false, HelpText = "The fuzzy pattern for files to include")]
        public string IncludeFuzzy { get; }

        [Option("shallow", Required = false, HelpText = "Check if chunks exist without further validation", Default = false)]
        public bool Shallow { get; }
    }
}
