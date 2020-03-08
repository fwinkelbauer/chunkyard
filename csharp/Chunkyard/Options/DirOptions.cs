using CommandLine;

namespace Chunkyard.Options
{
    [Verb("dir", HelpText = "Lists all files in a snapshot")]
    public class DirOptions
    {
        public DirOptions(string repository, string includeRegex, string logId)
        {
            Repository = repository;
            IncludeRegex = includeRegex;
            LogId = logId;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository", Default = Command.DefaultRepository)]
        public string Repository { get; }

        [Option('i', "include", Required = false, HelpText = "The include regex", Default = ".*")]
        public string IncludeRegex { get; }

        [Option('l', "log-uri", Required = false, HelpText = "The log URI", Default = Command.DefaultLogId)]
        public string LogId { get; }
    }
}
