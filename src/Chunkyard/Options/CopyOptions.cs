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

        [Option('s', "source", Required = true, HelpText = "The source repository path")]
        public string SourceRepository { get; }

        [Option('d', "destination", Required = true, HelpText = "The destination repository path")]
        public string DestinationRepository { get; }
    }
}
