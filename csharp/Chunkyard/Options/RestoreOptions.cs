using CommandLine;

namespace Chunkyard.Options
{
    [Verb("restore", HelpText = "Restores a snapshot")]
    public class RestoreOptions
    {
        public RestoreOptions(string repository, string directory, string includeRegex, string logId, bool overwrite)
        {
            Repository = repository;
            Directory = directory;
            IncludeRegex = includeRegex;
            LogId = logId;
            Overwrite = overwrite;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository", Default = Command.RepositoryDirectoryName)]
        public string Repository { get; }

        [Option('d', "directory", Required = false, HelpText = "The directory to restore into", Default = Command.RootDirectoryName)]
        public string Directory { get; }

        [Option('i', "include", Required = false, HelpText = "The include regex", Default = ".*")]
        public string IncludeRegex { get; }

        [Option('l', "log-uri", Required = false, HelpText = "The log URI", Default = Command.DefaultLogId)]
        public string LogId { get; }

        [Option('o', "overwrite", Required = false, HelpText = "If files should overwritten", Default = false)]
        public bool Overwrite { get; }
    }
}
