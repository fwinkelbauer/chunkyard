using CommandLine;

namespace Chunkyard.Cli
{
    [Verb("copy", HelpText = "Copy/Mirror snapshots from one repository to another.")]
    public class CopyOptions
    {
        public CopyOptions(
            string sourceRepository,
            string destinationRepository,
            bool mirror)
        {
            SourceRepository = sourceRepository;
            DestinationRepository = destinationRepository;
            Mirror = mirror;
        }

        [Option('s', "source", Required = true, HelpText = "The source repository path")]
        public string SourceRepository { get; }

        [Option('d', "destination", Required = true, HelpText = "The destination repository path")]
        public string DestinationRepository { get; }

        [Option("mirror", Required = false, HelpText = "Let the destination mirror the source repository", Default = false)]
        public bool Mirror { get; }
    }
}
