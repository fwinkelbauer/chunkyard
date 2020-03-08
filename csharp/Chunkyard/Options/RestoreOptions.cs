using CommandLine;

namespace Chunkyard.Options
{
    [Verb("restore", HelpText = "Restores a snapshot")]
    public class RestoreOptions
    {
        public RestoreOptions(string repository, string directory, string includeRegex, string logId)
        {
            Repository = repository;
            Directory = directory;
            IncludeRegex = includeRegex;
            LogId = logId;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository", Default = Command.DefaultRepository)]
        public string Repository { get; }

        [Option('d', "directory", Required = true, HelpText = "The directory to restore into")]
        public string Directory { get; }

        [Option('i', "include", Required = false, HelpText = "The include regex", Default = ".*")]
        public string IncludeRegex { get; }

        [Option('l', "log-uri", Required = false, HelpText = "The log URI", Default = Command.DefaultLogId)]
        public string LogId { get; }
    }
}
