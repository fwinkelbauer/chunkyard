using CommandLine;

namespace Chunkyard.Options
{
    [Verb("restore", HelpText = "Restore a snapshot.")]
    public class RestoreOptions
    {
        public RestoreOptions(
            string repository,
            string directory,
            string includeFuzzy,
            int logPosition,
            bool overwrite)
        {
            Repository = repository;
            Directory = directory;
            IncludeFuzzy = includeFuzzy ?? string.Empty;
            LogPosition = logPosition;
            Overwrite = overwrite;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository")]
        public string Repository { get; }

        [Option('d', "directory", Required = true, HelpText = "The directory to restore into")]
        public string Directory { get; }

        [Option('i', "include", Required = false, HelpText = "The include fuzzy pattern")]
        public string IncludeFuzzy { get; }

        [Option('l', "log-position", Required = false, HelpText = "The log position", Default = CLI.LatestLogPosition)]
        public int LogPosition { get; }

        [Option('o', "overwrite", Required = false, HelpText = "If files should be overwritten", Default = false)]
        public bool Overwrite { get; }
    }
}
