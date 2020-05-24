using CommandLine;

namespace Chunkyard.Options
{
    [Verb("show", HelpText = "Show the content of a snapshot")]
    public class ShowOptions
    {
        public ShowOptions(
            string repository,
            int logPosition,
            string includeFuzzy)
        {
            Repository = repository;
            LogPosition = logPosition;
            IncludeFuzzy = includeFuzzy ?? string.Empty;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository")]
        public string Repository { get; }

        [Option('l', "log-position", Required = false, HelpText = "The log position", Default = Command.LatestLogPosition)]
        public int LogPosition { get; }

        [Option('i', "include", Required = false, HelpText = "The include fuzzy pattern")]
        public string IncludeFuzzy { get; }
    }
}
