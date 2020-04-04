using CommandLine;

namespace Chunkyard.Options
{
    [Verb("pull", HelpText = "Pulls the content of a snapshot from one repository to another repository")]
    public class PullOptions
    {
        public PullOptions(string sourceRepository, string destinationRepository)
        {
            SourceRepository = sourceRepository;
            DestinationRepository = destinationRepository;
        }

        [Option('s', "source", Required = false, HelpText = "The source repository", Default = Command.RemoteRepository)]
        public string SourceRepository { get; }

        [Option('d', "destination", Required = false, HelpText = "The destination repository", Default = Command.LocalRepository)]
        public string DestinationRepository { get; }
    }
}
