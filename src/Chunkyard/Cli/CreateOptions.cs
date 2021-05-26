using System.Collections.Generic;
using CommandLine;

namespace Chunkyard.Cli
{
    [Verb("create", HelpText = "Create a new snapshot.")]
    public class CreateOptions
    {
        public CreateOptions(
            string repository,
            IEnumerable<string> files,
            IEnumerable<string> excludePatterns,
            IEnumerable<string> scanPatterns)
        {
            Repository = repository;
            Files = files;
            ExcludePatterns = excludePatterns;
            ScanPatterns = scanPatterns;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option('f', "files", Required = true, HelpText = "The files and directories to include")]
        public IEnumerable<string> Files { get; }

        [Option('e', "exclude", Required = false, HelpText = "The fuzzy patterns for files to exclude")]
        public IEnumerable<string> ExcludePatterns { get; }

        [Option("scan", Required = false, HelpText = "The fuzzy patterns for files to always inspect for changes")]
        public IEnumerable<string> ScanPatterns { get; }
    }
}
