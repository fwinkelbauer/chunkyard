using CommandLine;

namespace Chunkyard.Options
{
    [Verb("verify", HelpText = "Verify a snapshot")]
    public class VerifyOptions
    {
        public VerifyOptions(
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

        [Option('r', "repository", Required = true, HelpText = "The repository")]
        public string Repository { get; }

        [Option('l', "log-position", Required = false, HelpText = "The log position", Default = Command.LatestLogPosition)]
        public int LogPosition { get; }

        [Option('i', "include", Required = false, HelpText = "The include fuzzy pattern", Default = ".*")]
        public string IncludeFuzzy { get; }

        [Option('s', "shallow", Required = false, HelpText = "Do not verify chunk hashes", Default = false)]
        public bool Shallow { get; }
    }
}
