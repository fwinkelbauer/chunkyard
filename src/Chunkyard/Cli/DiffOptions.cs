using CommandLine;

namespace Chunkyard.Cli
{
    [Verb("diff", HelpText = "Shows the difference between two snapshots.")]
    public class DiffOptions
    {
        public DiffOptions(
            string repository,
            int firstLogPosition,
            int secondLogPosition)
        {
            Repository = repository;
            FirstLogPosition = firstLogPosition;
            SecondLogPosition = secondLogPosition;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option('f', "first", Required = false, HelpText = "The first snapshot ID", Default = Commands.LatestLogPosition - 1)]
        public int FirstLogPosition { get; }

        [Option('s', "second", Required = false, HelpText = "The second snapshot ID", Default = Commands.LatestLogPosition)]
        public int SecondLogPosition { get; }
    }
}
