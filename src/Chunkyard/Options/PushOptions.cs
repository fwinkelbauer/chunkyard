using CommandLine;

namespace Chunkyard.Options
{
    [Verb("push", HelpText = "Push snapshots to a repository.")]
    public class PushOptions
    {
        public PushOptions(
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
