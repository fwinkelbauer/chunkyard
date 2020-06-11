using System.Collections.Generic;
using CommandLine;

namespace Chunkyard.Options
{
    [Verb("create", HelpText = "Create a new snapshot.")]
    public class CreateOptions
    {
        public CreateOptions(
            string repository,
            bool cached,
            IEnumerable<string> files,
            IEnumerable<string> excludePatterns,
            int min,
            int avg,
            int max)
        {
            Repository = repository;
            Cached = cached;
            Files = new List<string>(files);
            ExcludePatterns = excludePatterns == null
                ? new List<string>()
                : new List<string>(excludePatterns);

            Min = min;
            Avg = avg;
            Max = max;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository")]
        public string Repository { get; }

        [Option('c', "cached", Required = false, HelpText = "Use a file cache to improve performance", Default = false)]
        public bool Cached { get; }

        [Option('f', "files", Required = true, HelpText = "The files and directories to include")]
        public IEnumerable<string> Files { get; }

        [Option('e', "exclude", Required = false, HelpText = "The exclude fuzzy patterns")]
        public IEnumerable<string> ExcludePatterns { get; }

        [Option("min", Required = false, HelpText = "The minimum chunk size", Default = FastCdc.DefaultMin)]
        public int Min { get; }

        [Option("avg", Required = false, HelpText = "The average chunk size", Default = FastCdc.DefaultAvg)]
        public int Avg { get; }

        [Option("max", Required = false, HelpText = "The maximum chunk size", Default = FastCdc.DefaultMax)]
        public int Max { get; }
    }
}
