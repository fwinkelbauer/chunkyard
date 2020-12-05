using System.Collections.Generic;
using CommandLine;

namespace Chunkyard.Options
{
    [Verb("create", HelpText = "Create a new snapshot and perform a shallow check.")]
    public class CreateOptions
    {
        public CreateOptions(
            string repository,
            IEnumerable<string> files,
            IEnumerable<string> excludePatterns,
            bool cached,
            int min,
            int avg,
            int max)
        {
            Repository = repository;
            Files = files;
            ExcludePatterns = excludePatterns;
            Cached = cached;
            Min = min;
            Avg = avg;
            Max = max;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option('f', "files", Required = true, HelpText = "The files and directories to include")]
        public IEnumerable<string> Files { get; }

        [Option('e', "exclude", Required = false, HelpText = "The fuzzy patterns for files to exclude")]
        public IEnumerable<string> ExcludePatterns { get; }

        [Option("cached", Required = false, HelpText = "Use a file cache to improve performance", Default = false)]
        public bool Cached { get; }

        [Option("min", Required = false, HelpText = "The minimum chunk size", Default = FastCdc.DefaultMin)]
        public int Min { get; }

        [Option("avg", Required = false, HelpText = "The average chunk size", Default = FastCdc.DefaultAvg)]
        public int Avg { get; }

        [Option("max", Required = false, HelpText = "The maximum chunk size", Default = FastCdc.DefaultMax)]
        public int Max { get; }
    }
}
