using CommandLine;

namespace Chunkyard.Options
{
    [Verb("dir", HelpText = "Lists all files in a snapshot")]
    public class DirOptions
    {
        public DirOptions(string repository, string includeRegex, int logPosition)
        {
            Repository = repository;
            IncludeRegex = includeRegex;
            LogPosition = logPosition;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository", Default = Command.RepositoryDirectoryName)]
        public string Repository { get; }

        [Option('i', "include", Required = false, HelpText = "The include regex", Default = ".*")]
        public string IncludeRegex { get; }

        [Option('l', "log-position", Required = false, HelpText = "The log URI", Default = Command.DefaultLogPosition)]
        public int LogPosition { get; }
    }
}
