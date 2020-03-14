using CommandLine;

namespace Chunkyard.Options
{
    [Verb("verify", HelpText = "Verify a snapshot")]
    public class VerifyOptions
    {
        public VerifyOptions(string repository, int logPosition, string includeRegex)
        {
            Repository = repository;
            LogPosition = logPosition;
            IncludeRegex = includeRegex;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository", Default = Command.RepositoryDirectoryName)]
        public string Repository { get; }

        [Option('l', "log-position", Required = false, HelpText = "The log URI", Default = Command.DefaultLogPosition)]
        public int LogPosition { get; }

        [Option('i', "include", Required = false, HelpText = "The include regex", Default = ".*")]
        public string IncludeRegex { get; }
    }
}
