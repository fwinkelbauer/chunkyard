using CommandLine;

namespace Chunkyard.Options
{
    [Verb("verify", HelpText = "Verify a snapshot")]
    public class VerifyOptions
    {
        public VerifyOptions(string repository, int logPosition, string includeRegex, bool shallow)
        {
            Repository = repository;
            LogPosition = logPosition;
            IncludeRegex = includeRegex;
            Shallow = shallow;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository", Default = Command.RepositoryDirectoryName)]
        public string Repository { get; }

        [Option('l', "log-position", Required = false, HelpText = "The log position", Default = Command.DefaultLogPosition)]
        public int LogPosition { get; }

        [Option('i', "include", Required = false, HelpText = "The include regex", Default = ".*")]
        public string IncludeRegex { get; }

        [Option('s', "shallow", Required = false, HelpText = "Do not verify chunk hashes", Default = false)]
        public bool Shallow { get; }
    }
}
