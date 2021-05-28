using System.Collections.Generic;
using Chunkyard.Core;
using CommandLine;

namespace Chunkyard.Cli
{
    [Verb("preview", HelpText = "Preview the output of a create command.")]
    public class PreviewOptions
    {
        public PreviewOptions(
            string repository,
            IEnumerable<string> files,
            IEnumerable<string> excludePatterns,
            int snapshotId)
        {
            Repository = repository;
            Files = files;
            ExcludePatterns = excludePatterns;
            SnapshotId = snapshotId;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option('f', "files", Required = true, HelpText = "The files and directories to include")]
        public IEnumerable<string> Files { get; }

        [Option('e', "exclude", Required = false, HelpText = "The fuzzy patterns for files to exclude")]
        public IEnumerable<string> ExcludePatterns { get; }

        [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID", Default = SnapshotStore.LatestSnapshotId)]
        public int SnapshotId { get; }
    }
}
