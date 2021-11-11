namespace Chunkyard.Cli
{
    [Verb("remove", HelpText = "Remove a snapshot.")]
    public class RemoveOptions
    {
        public RemoveOptions(
            string repository,
            int snapshotId)
        {
            Repository = repository;
            SnapshotId = snapshotId;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option('s', "snapshot", Required = true, HelpText = "The snapshot ID")]
        public int SnapshotId { get; }
    }
}
