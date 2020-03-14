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

        [Option('s', "source", Required = true, HelpText = "The source repository")]
        public string SourceRepository { get; }

        [Option('d', "destination", Required = false, HelpText = "The destination repository", Default = Command.RepositoryDirectoryName)]
        public string DestinationRepository { get; }
    }
}
