using CommandLine;

namespace Chunkyard.Options
{
    [Verb("verify", HelpText = "Verify a snapshot")]
    public class VerifyOptions
    {
        public VerifyOptions(string repository, string logId)
        {
            Repository = repository;
            LogId = logId;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository", Default = Command.DefaultRepository)]
        public string Repository { get; }

        [Option('l', "log-uri", Required = false, HelpText = "The log URI", Default = Command.DefaultLogId)]
        public string LogId { get; }
    }
}
