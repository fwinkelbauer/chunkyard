using Chunkyard.Core;
using CommandLine;

namespace Chunkyard.Cli
{
    [Verb("diff", HelpText = "Shows the difference between two snapshots.")]
    public class DiffOptions
    {
        public DiffOptions(
            string repository,
            int firstSnapshotId,
            int secondSnapshotId)
        {
            Repository = repository;
            FirstSnapshotId = firstSnapshotId;
            SecondSnapshotId = secondSnapshotId;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option('f', "first", Required = false, HelpText = "The first snapshot ID", Default = SnapshotStore.LastSnapshotId - 1)]
        public int FirstSnapshotId { get; }

        [Option('s', "second", Required = false, HelpText = "The second snapshot ID", Default = SnapshotStore.LastSnapshotId)]
        public int SecondSnapshotId { get; }
    }
}
