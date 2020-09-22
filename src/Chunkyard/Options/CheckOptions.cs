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
            IncludeFuzzy = includeFuzzy ?? string.Empty;
            Shallow = shallow;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository")]
        public string Repository { get; }

        [Option('l', "log-position", Required = false, HelpText = "The log position", Default = CLI.LatestLogPosition)]
        public int LogPosition { get; }

        [Option('i', "include", Required = false, HelpText = "The include fuzzy pattern")]
        public string IncludeFuzzy { get; }

        [Option('s', "shallow", Required = false, HelpText = "Do not verify chunk hashes", Default = false)]
        public bool Shallow { get; }
    }
}
