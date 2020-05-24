using System.Collections.Generic;
using CommandLine;

namespace Chunkyard.Options
{
    [Verb("backup", HelpText = "Create a new snapshot")]
    public class BackupOptions
    {
        public BackupOptions(
            string repository,
            bool cached,
            IEnumerable<string> files,
            IEnumerable<string> excludePatterns)
        {
            Repository = repository;
            Cached = cached;
            Files = new List<string>(files);
            ExcludePatterns = excludePatterns == null
                ? new List<string>()
                : new List<string>(excludePatterns);
        }

        [Option('r', "repository", Required = true, HelpText = "The repository")]
        public string Repository { get; }

        [Option('c', "cached", Required = false, HelpText = "Use a file cache to improve performance", Default = false)]
        public bool Cached { get; }

        [Option('f', "files", Required = true, HelpText = "The files and directories to include")]
        public IEnumerable<string> Files { get; }

        [Option('e', "exclude", Required = false, HelpText = "The exclude fuzzy patterns")]
        public IEnumerable<string> ExcludePatterns { get; }
    }
}
