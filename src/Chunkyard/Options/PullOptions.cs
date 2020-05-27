using CommandLine;

namespace Chunkyard.Options
{
    [Verb("pull", HelpText = "Pull snapshots from a repository.")]
    public class PullOptions
    {
        public PullOptions(
            string sourceRepository,
            string destinationRepository)
        {
            SourceRepository = sourceRepository;
            DestinationRepository = destinationRepository;
        }

        [Option('s', "source-repository", Required = true, HelpText = "The source repository")]
        public string SourceRepository { get; }

        [Option('d', "destination-repository", Required = true, HelpText = "The destination repository")]
        public string DestinationRepository { get; }
    }
}
