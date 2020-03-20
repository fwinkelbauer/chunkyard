using CommandLine;

namespace Chunkyard.Options
{
    [Verb("cat", HelpText = "Prints a snapshot and its content")]
    public class CatOptions
    {
        public CatOptions(string repository, string includeRegex, int logPosition)
        {
            Repository = repository;
            IncludeRegex = includeRegex;
            LogPosition = logPosition;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository", Default = Command.RepositoryDirectoryName)]
        public string Repository { get; }

        [Option('i', "include", Required = false, HelpText = "The include regex", Default = ".*")]
        public string IncludeRegex { get; }

        [Option('l', "log-position", Required = false, HelpText = "The log position", Default = Command.DefaultLogPosition)]
        public int LogPosition { get; }
    }
}
