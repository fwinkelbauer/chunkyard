using CommandLine;

namespace Chunkyard.Options
{
    [Verb("push", HelpText = "Pushes the content of a snapshot in a given log from one repository to another repository")]
    public class PushOptions
    {
        public PushOptions(string sourceRepository, string destinationRepository, string logName)
        {
            SourceRepository = sourceRepository;
            DestinationRepository = destinationRepository;
            LogName = logName;
        }

        [Option('s', "source", Required = false, HelpText = "The source repository", Default = Command.RepositoryDirectoryName)]
        public string SourceRepository { get; }

        [Option('d', "destination", Required = true, HelpText = "The destination repository")]
        public string DestinationRepository { get; }

        [Option('l', "log-name", Required = false, HelpText = "The log name", Default = Command.DefaultLogName)]
        public string LogName { get; }
    }
}
