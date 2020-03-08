using CommandLine;

namespace Chunkyard.Options
{
    [Verb("pull", HelpText = "Pulls the content of a snapshot in a given log from one repository to another repository")]
    public class PullOptions
    {
        public PullOptions(string sourceRepository, string destinationRepository, string logName)
        {
            SourceRepository = sourceRepository;
            DestinationRepository = destinationRepository;
            LogName = logName;
        }

        [Option('s', "source", Required = true, HelpText = "The source repository")]
        public string SourceRepository { get; }

        [Option('d', "destination", Required = true, HelpText = "The destination repository")]
        public string DestinationRepository { get; }

        [Option('l', "log-name", Required = false, HelpText = "The log name", Default = Command.DefaultLogName)]
        public string LogName { get; }
    }
}
