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
            IncludeFuzzy = includeFuzzy;
            LogPosition = logPosition;
            Overwrite = overwrite;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option('d', "directory", Required = false, HelpText = "The directory to restore into", Default = ".")]
        public string Directory { get; }

        [Option('i', "include", Required = false, HelpText = "The fuzzy pattern for files to include")]
        public string IncludeFuzzy { get; }

        [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID", Default = Cli.LatestLogPosition)]
        public int LogPosition { get; }

        [Option("overwrite", Required = false, HelpText = "If files should be overwritten", Default = false)]
        public bool Overwrite { get; }
    }
}
