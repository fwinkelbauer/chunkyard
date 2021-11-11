using System.Collections.Generic;
using Chunkyard.Core;
using CommandLine;

namespace Chunkyard.Cli
{
    [Verb("show", HelpText = "Show the content of a snapshot.")]
    public class ShowOptions
    {
        public ShowOptions(
            string repository,
            int snapshotId,
            IEnumerable<string> includePatterns,
            bool contentOnly)
        {
            Repository = repository;
            SnapshotId = snapshotId;
            IncludePatterns = includePatterns;
            ContentOnly = contentOnly;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option('s', "snapshot", Required = false, HelpText = "The snapshot ID", Default = SnapshotStore.LatestSnapshotId)]
        public int SnapshotId { get; }

        [Option('i', "include", Required = false, HelpText = "The fuzzy patterns for files to include")]
        public IEnumerable<string> IncludePatterns { get; }

        [Option("content-only", Required = false, HelpText = "Show content", Default = false)]
        public bool ContentOnly { get; }
    }
}
