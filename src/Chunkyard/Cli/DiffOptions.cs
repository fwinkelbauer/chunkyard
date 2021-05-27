using Chunkyard.Core;
using CommandLine;

namespace Chunkyard.Cli
{
    [Verb("diff", HelpText = "Show the difference between two snapshots.")]
    public class DiffOptions
    {
        public DiffOptions(
            string repository,
            int firstSnapshotId,
            int secondSnapshotId,
            bool contentOnly)
        {
            Repository = repository;
            FirstSnapshotId = firstSnapshotId;
            SecondSnapshotId = secondSnapshotId;
            ContentOnly = contentOnly;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option('f', "first", Required = false, HelpText = "The first snapshot ID", Default = SnapshotStore.SecondLatestSnapshotId)]
        public int FirstSnapshotId { get; }

        [Option('s', "second", Required = false, HelpText = "The second snapshot ID", Default = SnapshotStore.LatestSnapshotId)]
        public int SecondSnapshotId { get; }

        [Option('c', "content-only", Required = false, HelpText = "The second snapshot ID", Default = false)]
        public bool ContentOnly { get; }
    }
}
