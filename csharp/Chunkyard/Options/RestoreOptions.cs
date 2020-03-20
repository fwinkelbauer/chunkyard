using CommandLine;

namespace Chunkyard.Options
{
    [Verb("restore", HelpText = "Restores a snapshot")]
    public class RestoreOptions
    {
        public RestoreOptions(string repository, string directory, string includeFuzzy, int logPosition, bool overwrite)
        {
            Repository = repository;
            Directory = directory;
            IncludeFuzzy = includeFuzzy;
            LogPosition = logPosition;
            Overwrite = overwrite;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository", Default = Command.RepositoryDirectoryName)]
        public string Repository { get; }

        [Option('d', "directory", Required = false, HelpText = "The directory to restore into", Default = Command.RootDirectoryName)]
        public string Directory { get; }

        [Option('i', "include", Required = false, HelpText = "The include fuzzy pattern", Default = ".*")]
        public string IncludeFuzzy { get; }

        [Option('l', "log-position", Required = false, HelpText = "The log position", Default = Command.DefaultLogPosition)]
        public int LogPosition { get; }

        [Option('o', "overwrite", Required = false, HelpText = "If files should overwritten", Default = false)]
        public bool Overwrite { get; }
    }
}
