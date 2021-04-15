using System.Collections.Generic;
using Chunkyard.Core;
using CommandLine;

namespace Chunkyard.Cli
{
    [Verb("restore", HelpText = "Restore a snapshot.")]
    public class RestoreOptions
    {
        public RestoreOptions(
            string repository,
            string directory,
            IEnumerable<string> includePatterns,
            int snapshotId,
            bool overwrite)
        {
            Repository = repository;
            Directory = directory;
            IncludePatterns = includePatterns;
            SnapshotId = snapshotId;
            Overwrite = overwrite;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option('d', "directory", Required = false, HelpText = "The directory to restore into", Default = ".")]
        public string Directory { get; }

        [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for files to include")]
        public IEnumerable<string> IncludePatterns { get; }

        [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID", Default = SnapshotStore.LatestSnapshotId)]
        public int SnapshotId { get; }

        [Option("overwrite", Required = false, HelpText = "If files should be overwritten", Default = false)]
        public bool Overwrite { get; }
    }
}
