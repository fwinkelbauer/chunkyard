using CommandLine;

namespace Chunkyard.Options
{
    [Verb("cat", HelpText = "Prints a snapshot and its content")]
    public class CatOptions
    {
        public CatOptions(string repository, string includeFuzzy, int logPosition)
        {
            Repository = repository;
            IncludeFuzzy = includeFuzzy;
            LogPosition = logPosition;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository", Default = Command.LocalRepository)]
        public string Repository { get; }

        [Option('i', "include", Required = false, HelpText = "The include fuzzy pattern", Default = ".*")]
        public string IncludeFuzzy { get; }

        [Option('l', "log-position", Required = false, HelpText = "The log position", Default = Command.DefaultLogPosition)]
        public int LogPosition { get; }
    }
}
