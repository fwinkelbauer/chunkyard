using CommandLine;

namespace Chunkyard.Options
{
    [Verb("copy", HelpText = "Copy snapshots from one repository to another.")]
    public class CopyOptions
    {
        public CopyOptions(
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
